using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Impacts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    // A physics ray on the weapon's layers. Triggers count only when they carry a weapon target: a target may have a
    // trigger collider so that it pushes nothing, while a trigger with no target on it (a field structure's volume)
    // lets the shot through. The shooter's own body is passed through. What shows the strike on the struck surface,
    // and a struck body that reacts to shots, travel with the hit.
    public sealed class PhysicsWeaponRaycast : IWeaponRaycast {
        // Enough for the triggers a long ray crosses through a dense rock field before the nearest real surface.
        private const int MAX_RAY_HITS = 32;

        private readonly LayerMask _hitLayers;
        private readonly IWeaponImpactLookup _impactLookup;
        private readonly RaycastHit[] _hits = new RaycastHit[MAX_RAY_HITS];

        public PhysicsWeaponRaycast(LayerMask hitLayers, IWeaponImpactLookup impactLookup) {
            _hitLayers = hitLayers;
            _impactLookup = impactLookup;
        }

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, uint ignoredReceiverId, out WeaponRaycastHit hit) {
            hit = default;
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance, _hitLayers, QueryTriggerInteraction.Collide);
            if (!TryFindNearestStruck(count, ignoredReceiverId, out RaycastHit physicsHit, out IWeaponTargetCollider target))
                return false;

            bool hasTarget = target != null;
            physicsHit.collider.TryGetComponent(out IWeaponImpactFeedback feedback);
            bool hasReceiver = TryFindReceiver(physicsHit, out IWeaponImpactReceiver receiver, out int partIndex);
            hit = new WeaponRaycastHit(physicsHit.point, physicsHit.normal, hasTarget, hasTarget ? target.TargetId : 0, feedback,
                hasReceiver, hasReceiver ? receiver.NetworkId : 0, partIndex);
            return true;
        }

        private bool TryFindNearestStruck(int count, uint ignoredReceiverId, out RaycastHit nearest, out IWeaponTargetCollider nearestTarget) {
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

                if (IsIgnoredBody(candidate, ignoredReceiverId))
                    continue;

                nearest = candidate;
                nearestTarget = hasTarget ? target : null;
                hasNearest = true;
            }

            return hasNearest;
        }

        private bool IsIgnoredBody(in RaycastHit candidate, uint ignoredReceiverId) =>
            ignoredReceiverId != 0
            && TryFindReceiver(candidate, out IWeaponImpactReceiver receiver, out _)
            && receiver.NetworkId == ignoredReceiverId;

        private bool TryFindReceiver(in RaycastHit hit, out IWeaponImpactReceiver receiver, out int partIndex) {
            receiver = null;
            partIndex = -1;
            return _impactLookup != null && _impactLookup.TryFindByPart(hit.collider.attachedRigidbody, out receiver, out partIndex);
        }
    }
}
