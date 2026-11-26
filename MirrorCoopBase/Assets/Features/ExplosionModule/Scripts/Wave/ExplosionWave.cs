using System.Collections.Generic;
using Features.ExplosionModule.Configuration;
using Features.ExplosionModule.Contracts;
using UnityEngine;

namespace Features.ExplosionModule.Wave {
    // One explosion's wave on this peer: a sphere growing from the centre at the preset's speed. Each physics step it
    // reaches what lies inside its new radius and was not reached yet: receivers on every peer, loose rigidbodies only
    // on the server (they are simulated there and synced to the clients). Farther things are reached later.
    internal sealed class ExplosionWave {
        private readonly Vector3 _center;
        private readonly ExplosionPreset _preset;
        private readonly HashSet<IExplosionReceiver> _reachedReceivers = new();
        private readonly HashSet<Rigidbody> _thrownBodies = new();
        private float _elapsed;

        public ExplosionWave(Vector3 center, ExplosionPreset preset) {
            _center = center;
            _preset = preset;
        }

        public bool IsOver { get; private set; }

        public void Step(float deltaTime, ExplosionReceiverRegistry registry, ExplosionStepContext context) {
            _elapsed += deltaTime;
            float radius = Mathf.Min(_preset.Radius, _preset.WaveSpeed * _elapsed);
            ReachReceivers(radius, registry);
            if (context.ThrowsLooseBodies)
                ThrowLooseBodies(radius, registry, context);

            IsOver = radius >= _preset.Radius;
        }

        private void ReachReceivers(float radius, ExplosionReceiverRegistry registry) {
            IReadOnlyList<IExplosionReceiver> receivers = registry.Receivers;
            for (int i = receivers.Count - 1; i >= 0; i--) {
                if (i >= receivers.Count)
                    continue;

                IExplosionReceiver receiver = receivers[i];
                float distance = Vector3.Distance(receiver.Center, _center);
                if (distance > radius || _reachedReceivers.Add(receiver) == false)
                    continue;

                receiver.ReceiveExplosion(HitAt(distance));
            }
        }

        private void ThrowLooseBodies(float radius, ExplosionReceiverRegistry registry, ExplosionStepContext context) {
            Collider[] buffer = context.OverlapBuffer;
            int count = Physics.OverlapSphereNonAlloc(_center, radius, buffer, context.AffectedLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) {
                Rigidbody body = buffer[i].attachedRigidbody;
                if (body == null || body.isKinematic || registry.IsOwnedPart(body) || _thrownBodies.Add(body) == false)
                    continue;

                // The side facing the centre gets the push, so the body also spins away.
                Vector3 point = buffer[i].ClosestPointOnBounds(_center);
                ExplosionHit hit = HitAt(Vector3.Distance(point, _center));
                body.AddForceAtPosition(hit.VelocityAt(body.worldCenterOfMass) * body.mass, point, ForceMode.Impulse);
            }
        }

        private ExplosionHit HitAt(float distance) {
            float share = _preset.ShareAt(distance);
            float knockdown = share >= _preset.KnockdownMinShare && share > 0f ? _preset.KnockdownSeconds : 0f;
            return new ExplosionHit(_center, _preset.CenterSpeed * share, _preset.UpwardShare, knockdown);
        }
    }
}
