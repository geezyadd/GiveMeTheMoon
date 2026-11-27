using System.Collections.Generic;
using Features.AudioModule;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Impacts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;
using Features.WeaponModule.Scripts.Projectiles;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Network {
    // Shows one shot on one peer, once: the weapon's feel (flash, kick, tracer, impact) and sounds, the struck surface's own
    // feedback, the struck body's reaction and, on a peer that is not the server, a copy of the projectile it launched.
    public sealed class WeaponShotPresenter {
        // A received shot's end point lies on the struck surface; the ray looking for it goes this far past it.
        private const float SURFACE_SEARCH_MARGIN = 0.05f;

        private readonly IWeaponImpactLookup _impactLookup;
        private readonly IProjectileSpawner _projectileSpawner;
        private readonly IAudioService _audioService;

        public WeaponShotPresenter(IWeaponImpactLookup impactLookup, IProjectileSpawner projectileSpawner, IAudioService audioService) {
            _impactLookup = impactLookup;
            _projectileSpawner = projectileSpawner;
            _audioService = audioService;
        }

        public void Present(WeaponDefinition weapon, IWeaponFeel feel, in WeaponShotOutcome outcome, ulong projectileId, bool spawnsCopy,
                            IReadOnlyList<Collider> ignoredByProjectile) {
            feel?.PresentShot(outcome);
            PlaySounds(weapon.FeelProfile, outcome);
            if (outcome.ImpactFeedback != null)
                outcome.ImpactFeedback.PresentImpact(outcome.EndPoint, outcome.Normal);

            Vector3 direction = DirectionOf(outcome);
            if (outcome.HasReceiver)
                PushReceiver(weapon, outcome, direction);

            if (weapon.Projectile != null && spawnsCopy)
                _projectileSpawner.SpawnCopy(weapon.Projectile, outcome.Shot.Origin, direction * weapon.Projectile.Speed, projectileId,
                    ignoredByProjectile);
        }

        // Another peer only knows where the shot ended: the surface there shows the strike if it can.
        public WeaponShotOutcome Rebuild(WeaponDefinition weapon, in WeaponShotMessage message) {
            IWeaponImpactFeedback feedback = message.IsImpact ? FindFeedback(message) : null;
            WeaponShot shot = new WeaponShot(weapon.Id, message.Origin, message.Direction, weapon.RangeMeters);
            return WeaponShotOutcome.CreateReceived(shot, message.EndPoint, message.Normal, message.IsImpact, message.IsTargetHit,
                message.TargetId, feedback, message.HasReceiver, message.ReceiverId, message.ReceiverPartIndex);
        }

        private void PlaySounds(WeaponFeelProfile profile, in WeaponShotOutcome outcome) {
            if (profile == null)
                return;

            _audioService.PlayOneShot(profile.ShotSound, outcome.Shot.Origin);
            if (outcome.IsImpact)
                _audioService.PlayOneShot(profile.ImpactSound, outcome.EndPoint);
        }

        private void PushReceiver(WeaponDefinition weapon, in WeaponShotOutcome outcome, Vector3 direction) {
            if (weapon.ImpactPushSpeed <= 0f && weapon.ImpactKnockdownSeconds <= 0f)
                return;

            if (_impactLookup.TryGet(outcome.ReceiverId, out IWeaponImpactReceiver receiver))
                receiver.ReceiveImpact(new WeaponImpact(outcome.EndPoint, direction, weapon.ImpactPushSpeed, weapon.ImpactKnockdownSeconds,
                    outcome.ReceiverPartIndex));
        }

        private static IWeaponImpactFeedback FindFeedback(in WeaponShotMessage message) {
            Vector3 toEnd = message.EndPoint - message.Origin;
            float distance = toEnd.magnitude;
            if (distance <= 0f || Physics.Raycast(message.Origin, toEnd / distance, out RaycastHit hit, distance + SURFACE_SEARCH_MARGIN,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) == false)
                return null;

            return hit.collider.TryGetComponent(out IWeaponImpactFeedback feedback) ? feedback : null;
        }

        private static Vector3 DirectionOf(in WeaponShotOutcome outcome) {
            Vector3 toEnd = outcome.EndPoint - outcome.Shot.Origin;
            return toEnd.sqrMagnitude > 0f ? toEnd.normalized : outcome.Shot.Direction.normalized;
        }
    }
}
