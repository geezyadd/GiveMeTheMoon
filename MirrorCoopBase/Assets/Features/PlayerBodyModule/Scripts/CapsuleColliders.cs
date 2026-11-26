using System.Collections.Generic;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // Every collider of the player's capsule (its own and those of its children), not the ragdoll's. While the ragdoll
    // is the body they stay off: the capsule follows the ragdoll and would otherwise shove it. Others (a revive) may
    // turn them on again, so the ragdoll keeps them off every step.
    public sealed class CapsuleColliders {
        private readonly List<Collider> _colliders = new();
        private readonly List<bool> _wasEnabled = new();

        public CapsuleColliders(Transform capsule, Transform ragdoll) {
            foreach (Collider found in capsule.GetComponentsInChildren<Collider>(true))
                if (found.transform.IsChildOf(ragdoll) == false)
                    _colliders.Add(found);
        }

        public void TurnOff() {
            _wasEnabled.Clear();
            foreach (Collider capsuleCollider in _colliders) {
                _wasEnabled.Add(capsuleCollider != null && capsuleCollider.enabled);
                if (capsuleCollider != null)
                    capsuleCollider.enabled = false;
            }
        }

        public void KeepOff() {
            foreach (Collider capsuleCollider in _colliders)
                if (capsuleCollider != null && capsuleCollider.enabled)
                    capsuleCollider.enabled = false;
        }

        public void Restore() {
            for (int i = 0; i < _colliders.Count && i < _wasEnabled.Count; i++)
                if (_colliders[i] != null)
                    _colliders[i].enabled = _wasEnabled[i];
            _wasEnabled.Clear();
        }
    }
}
