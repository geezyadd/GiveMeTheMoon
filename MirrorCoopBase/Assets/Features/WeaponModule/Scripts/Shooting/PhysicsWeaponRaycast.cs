using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    // A physics ray on the weapon's layers. Triggers count: a weapon target may have a trigger collider so that it
    // pushes nothing.
    public sealed class PhysicsWeaponRaycast : IWeaponRaycast {
        private readonly LayerMask _hitLayers;

        public PhysicsWeaponRaycast(LayerMask hitLayers) =>
            _hitLayers = hitLayers;

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            hit = default;
            if (Physics.Raycast(origin, direction, out RaycastHit physicsHit, maxDistance, _hitLayers, QueryTriggerInteraction.Collide) == false)
                return false;

            bool hasTarget = physicsHit.collider.TryGetComponent(out IWeaponTargetCollider target);
            hit = new WeaponRaycastHit(physicsHit.point, physicsHit.normal, hasTarget, hasTarget ? target.TargetId : 0);
            return true;
        }
    }
}
