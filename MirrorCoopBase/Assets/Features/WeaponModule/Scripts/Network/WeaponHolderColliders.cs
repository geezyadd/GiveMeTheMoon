using System.Collections.Generic;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Impacts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Network {
    // The colliders a holder's own projectiles fly through: the holder's own body (every part of it) and the weapon
    // it holds, so a grenade never goes off in the hand that throws it.
    public sealed class WeaponHolderColliders {
        private readonly List<Collider> _colliders = new();
        private readonly List<Collider> _found = new();

        public IReadOnlyList<Collider> Colliders => _colliders;

        public void Collect(IWeaponImpactLookup impactLookup, uint holderBodyId, Object weapon) {
            _colliders.Clear();
            if (impactLookup.TryGet(holderBodyId, out IWeaponImpactReceiver body))
                foreach (Rigidbody part in body.Parts)
                    AddAll(part);

            if (weapon is Component component && component != null)
                AddFrom(component.GetComponentsInChildren<Collider>(true));
        }

        public void Clear() =>
            _colliders.Clear();

        private void AddAll(Rigidbody part) {
            if (part == null)
                return;

            part.GetComponents(_found);
            AddFrom(_found);
        }

        private void AddFrom(IReadOnlyList<Collider> colliders) {
            for (int i = 0; i < colliders.Count; i++)
                if (colliders[i] != null && _colliders.Contains(colliders[i]) == false)
                    _colliders.Add(colliders[i]);
        }
    }
}
