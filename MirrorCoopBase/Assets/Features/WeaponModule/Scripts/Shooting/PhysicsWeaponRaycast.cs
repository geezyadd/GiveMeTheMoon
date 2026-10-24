using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    // A physics ray on the weapon's layers. Triggers count only when they carry a weapon target: a target may have a
    // trigger collider so that it pushes nothing, while a trigger with no target on it (a field structure's volume)
    // lets the shot through. A struck target that shows impacts is told here, on every peer that casts the shot.
    public sealed class PhysicsWeaponRaycast : IWeaponRaycast {
        // Enough for the triggers a long ray crosses through a dense rock field before the nearest real surface.
        private const int MAX_RAY_HITS = 32;

        private readonly LayerMask _hitLayers;
        private readonly RaycastHit[] _hits = new RaycastHit[MAX_RAY_HITS];

        public PhysicsWeaponRaycast(LayerMask hitLayers) =>
            _hitLayers = hitLayers;

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            hit = default;
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance, _hitLayers, QueryTriggerInteraction.Collide);
            if (!TryFindNearestStruck(count, out RaycastHit physicsHit, out IWeaponTargetCollider target))
                return false;

            bool hasTarget = target != null;
            hit = new WeaponRaycastHit(physicsHit.point, physicsHit.normal, hasTarget, hasTarget ? target.TargetId : 0);
            if (physicsHit.collider.TryGetComponent(out IWeaponImpactFeedback feedback))
                feedback.PresentImpact(physicsHit.point, physicsHit.normal);

            return true;
        }

        private bool TryFindNearestStruck(int count, out RaycastHit nearest, out IWeaponTargetCollider nearestTarget) {
            nearest = default;
            nearestTarget = null;
            bool hasNearest = false;
            for (int i = 0; i < count; i++) {
                RaycastHit candidate = _hits[i];
                if (hasNearest && candidate.distance >= nearest.distance)
                    continue;

                bool hasTarget = candidate.collider.TryGetComponent(out IWeaponTargetCollider target);
                if (candidate.collider.isTrigger && !hasTarget)
                    continue;

                nearest = candidate;
                nearestTarget = hasTarget ? target : null;
                hasNearest = true;
            }

            return hasNearest;
        }
    }
}
