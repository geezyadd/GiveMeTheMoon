using System;
using UnityEngine;
using Zenject;

namespace Features.TargetModule {
    // Something other systems can find and act on (TargetKind): an attack goes for it and lands on it, eyes look at it.
    // It is listed in the TargetRegistry while on. Its aim point is where attacks and looks go; its parts are the
    // rigidbodies whose touch counts as touching it (a player's head limb). Reactions (a push, a flame, a particle, a
    // mob running away) are components that listen to Hit.
    public sealed class Target : MonoBehaviour {
        [SerializeField] private TargetKind _kinds = TargetKind.Hittable;
        [Tooltip("Where attacks and looks go (the middle of a head). Empty: this object.")]
        [SerializeField] private Transform _aimPoint;
        [Tooltip("The rigidbodies whose touch counts as touching this target.")]
        [SerializeField] private Rigidbody[] _parts = Array.Empty<Rigidbody>();
        [Tooltip("Only while its parts are simulated (not kinematic): a sleeping ragdoll, a dead player or a body nobody simulates here, is no target.")]
        [SerializeField] private bool _onlyWhileSimulated = true;

        private TargetRegistry _registry;

        // An attack landed on this target, on this peer.
        public event Action<TargetHit> Hit;

        public TargetKind Kinds => _kinds;
        public Transform AimPoint => _aimPoint != null ? _aimPoint : transform;
        public bool IsAvailable => isActiveAndEnabled && (_onlyWhileSimulated == false || IsSimulated());

        [Inject]
        private void Construct(TargetRegistry registry) {
            _registry = registry;
            if (isActiveAndEnabled)
                _registry.Add(this);
        }

        private void OnEnable() =>
            _registry?.Add(this);

        private void OnDisable() =>
            _registry?.Remove(this);

        public bool Is(TargetKind kind) =>
            kind != TargetKind.None && (_kinds & kind) == kind;

        public bool Owns(Rigidbody body) {
            if (body == null)
                return false;

            foreach (Rigidbody part in _parts)
                if (part == body)
                    return true;

            return false;
        }

        public void ReceiveHit(in TargetHit hit) =>
            Hit?.Invoke(hit);

        private bool IsSimulated() {
            foreach (Rigidbody part in _parts)
                if (part == null || part.isKinematic)
                    return false;

            return true;
        }
    }
}
