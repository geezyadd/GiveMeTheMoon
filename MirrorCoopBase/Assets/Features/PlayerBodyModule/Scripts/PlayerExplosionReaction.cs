using System.Collections.Generic;
using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using Features.ExplosionModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // A player's reaction to an explosion's wave, on every peer that simulates this player's ragdoll (the owner its real
    // body, the others their copies): every limb is thrown away from the centre and, close enough, the ragdoll goes
    // limp for the preset's time (RagdollKnockdown) while the movement leaves it alone. A seated player is first stood
    // up by the server (ShipModule's eject); the hit waits here until the seat lets go of the body, at most a moment.
    public sealed class PlayerExplosionReaction : MonoBehaviour, IExplosionReceiver {
        [SerializeField] private RagdollBody _ragdoll;
        [SerializeField] private RagdollKnockdown _knockdown;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("A hit that reaches a seated player waits this long, s, for the seat to let go before it is dropped.")]
        [SerializeField, Min(0f)] private float _heldWaitSeconds = 1f;

        private readonly List<Rigidbody> _parts = new();
        private IExplosionReceiverRegistry _registry;
        private ExplosionHit _pendingHit;
        private float _pendingSecondsLeft;

        public Vector3 Center => _ragdoll != null && _ragdoll.Root != null
            ? _ragdoll.Root.Rigidbody.position
            : transform.position;

        public IReadOnlyList<Rigidbody> Parts => _parts;

        [Inject]
        private void Construct(IExplosionReceiverRegistry registry) {
            _registry = registry;
            if (isActiveAndEnabled)
                _registry.Add(this);
        }

        private void Awake() {
            if (_movable != null && _movable.Body != null)
                _parts.Add(_movable.Body);

            if (_ragdoll == null)
                return;

            foreach (RagdollLimb limb in _ragdoll.Limbs)
                _parts.Add(limb.Rigidbody);
        }

        private void OnEnable() =>
            _registry?.Add(this);

        private void OnDisable() {
            _registry?.Remove(this);
            _pendingSecondsLeft = 0f;
        }

        public void ReceiveExplosion(ExplosionHit hit) {
            if (_movable.IsHeld) {
                _pendingHit = hit;
                _pendingSecondsLeft = _heldWaitSeconds;
                return;
            }

            Apply(hit);
        }

        private void FixedUpdate() {
            if (_pendingSecondsLeft > 0f) {
                _pendingSecondsLeft -= Time.fixedDeltaTime;
                if (_movable.IsHeld == false) {
                    _pendingSecondsLeft = 0f;
                    Apply(_pendingHit);
                }
            }

            _movable.SetKnockedDown(_knockdown != null && _knockdown.IsKnockedDown);
        }

        private void Apply(ExplosionHit hit) {
            // A sleeping ragdoll (a dead player, a copy nobody simulates here) is not thrown on this peer.
            if (_ragdoll == null || _ragdoll.Root == null || _ragdoll.Root.Rigidbody.isKinematic)
                return;

            if (hit.KnocksDown && _knockdown != null)
                _knockdown.KnockDown(hit.KnockdownSeconds);

            foreach (RagdollLimb limb in _ragdoll.Limbs)
                limb.Rigidbody.AddForce(hit.VelocityAt(limb.Rigidbody.worldCenterOfMass), ForceMode.VelocityChange);
        }
    }
}
