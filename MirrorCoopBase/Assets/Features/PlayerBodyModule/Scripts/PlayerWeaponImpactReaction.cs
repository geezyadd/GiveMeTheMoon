using System.Collections.Generic;
using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using Features.WeaponModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // A player's reaction to a shot that strikes them, on every peer that simulates this player's ragdoll (the owner
    // its real body, the others their copies): the struck limb gets the shot's velocity change at the struck point,
    // the whole body a share of it, and a shot that knocks down makes the ragdoll limp for its time (RagdollKnockdown;
    // PlayerExplosionReaction keeps the movement off while it lies). A shot on the capsule pushes the limb nearest to
    // where it struck. A seated player and a sleeping ragdoll are left alone.
    public sealed class PlayerWeaponImpactReaction : MonoBehaviour, IWeaponImpactReceiver {
        [SerializeField] private NetworkIdentity _identity;
        [SerializeField] private RagdollBody _ragdoll;
        [SerializeField] private RagdollKnockdown _knockdown;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("Every limb gets this share of the struck limb's velocity change, so the body moves, not only the limb.")]
        [SerializeField, Range(0f, 1f)] private float _bodyShare = 0.25f;
        [Tooltip("A shot that knocks down gives every limb this share instead.")]
        [SerializeField, Range(0f, 1f)] private float _knockdownBodyShare = 0.6f;

        private readonly List<Rigidbody> _parts = new();
        private readonly List<RagdollLimb> _limbs = new();
        private IWeaponImpactReceiverRegistry _registry;

        public uint NetworkId => _identity != null ? _identity.netId : 0;
        public IReadOnlyList<Rigidbody> Parts => _parts;

        [Inject]
        private void Construct(IWeaponImpactReceiverRegistry registry) {
            _registry = registry;
            if (isActiveAndEnabled)
                _registry.Add(this);
        }

        private void Awake() {
            if (_ragdoll != null)
                foreach (RagdollLimb limb in _ragdoll.Limbs) {
                    _limbs.Add(limb);
                    _parts.Add(limb.Rigidbody);
                }

            if (_movable != null && _movable.Body != null)
                _parts.Add(_movable.Body);
        }

        private void OnEnable() =>
            _registry?.Add(this);

        private void OnDisable() =>
            _registry?.Remove(this);

        public void ReceiveImpact(in WeaponImpact impact) {
            if (_ragdoll == null || _ragdoll.Root == null || _ragdoll.Root.Rigidbody.isKinematic || _movable.IsHeld)
                return;

            bool knocksDown = impact.KnockdownSeconds > 0f && _knockdown != null;
            if (knocksDown)
                _knockdown.KnockDown(impact.KnockdownSeconds);

            Vector3 push = impact.Direction.normalized * impact.PushSpeed;
            Vector3 bodyPush = push * (knocksDown ? _knockdownBodyShare : _bodyShare);
            foreach (RagdollLimb limb in _limbs)
                limb.Rigidbody.AddForce(bodyPush, ForceMode.VelocityChange);

            RagdollLimb struck = FindStruckLimb(impact);
            if (struck != null)
                struck.Rigidbody.AddForceAtPosition(push - bodyPush, impact.Point, ForceMode.VelocityChange);
        }

        private RagdollLimb FindStruckLimb(in WeaponImpact impact) {
            if (impact.PartIndex >= 0 && impact.PartIndex < _limbs.Count)
                return _limbs[impact.PartIndex];

            RagdollLimb nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (RagdollLimb limb in _limbs) {
                float distance = (limb.Rigidbody.worldCenterOfMass - impact.Point).sqrMagnitude;
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearest = limb;
                }
            }

            return nearest;
        }
    }
}
