using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    // A physics ray on the weapon's layers, thin or as thick as a sphere (a rope that catches what passes near its
    // line). Triggers count only when they carry a weapon target: a target may have a
    // trigger collider so that it pushes nothing, while a trigger with no target on it (a field structure's volume)
    // lets the shot through. What shows the strike on the struck surface travels with the hit; the shooter shows it.
    public sealed class PhysicsWeaponRaycast : IWeaponRaycast {
        // Enough for the triggers a long ray crosses through a dense rock field before the nearest real surface.
        private const int MAX_RAY_HITS = 32;

        private readonly LayerMask _hitLayers;
        private readonly float _radiusMeters;
        private readonly RaycastHit[] _hits = new RaycastHit[MAX_RAY_HITS];

        public PhysicsWeaponRaycast(LayerMask hitLayers, float radiusMeters = 0f) {
            _hitLayers = hitLayers;
            _radiusMeters = radiusMeters;
        }

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            hit = default;
            int count = CastNonAlloc(origin, direction, maxDistance);
            if (!TryFindNearestStruck(count, out RaycastHit physicsHit, out IWeaponTargetCollider target))
                return false;

            bool hasTarget = target != null;
            physicsHit.collider.TryGetComponent(out IWeaponImpactFeedback feedback);
            hit = new WeaponRaycastHit(physicsHit.point, physicsHit.normal, hasTarget, hasTarget ? target.TargetId : 0, feedback);
            return true;
        }

        private int CastNonAlloc(Vector3 origin, Vector3 direction, float maxDistance) =>
            _radiusMeters > 0f
                ? Physics.SphereCastNonAlloc(origin, _radiusMeters, direction, _hits, maxDistance, _hitLayers, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance, _hitLayers, QueryTriggerInteraction.Collide);

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
