using UnityEngine;

namespace Features.FloatingControllerModule {
    // Finds what a body stands on: a sphere cast that skips the body's own colliders and keeps the nearest hit,
    // plus the moving carrier (ship deck, platform) that hit belongs to. Shared by every body that hovers over
    // a support, whatever holds it up.
    public sealed class SupportProbe {
        private const float MinRadius = 0.05f;
        private const float MinCastDistance = 0.01f;

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private Collider _carrierLookupCollider;
        private ICarrier _carrierLookup;

        // The hit's distance is measured from the origin to the surface, the sphere radius included.
        public bool TryCast(Vector3 origin, Vector3 direction, float radius, float length, LayerMask mask,
            Transform self, Rigidbody selfBody, out RaycastHit nearestHit) {
            radius = Mathf.Max(radius, MinRadius);
            float castDistance = Mathf.Max(length - radius, MinCastDistance);
            int hits = Physics.SphereCastNonAlloc(origin, radius, direction, _hits, castDistance, mask,
                QueryTriggerInteraction.Ignore);

            nearestHit = default;
            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits; i++) {
                RaycastHit candidate = _hits[i];
                if (BelongsToSelf(candidate, self, selfBody) || candidate.distance >= nearest)
                    continue;

                nearest = candidate.distance;
                nearestHit = candidate;
                found = true;
            }

            if (found)
                nearestHit.distance += radius;
            return found;
        }

        // The moving carrier on the support's object or a parent of it, or null on still ground. Cached per
        // collider: a body asks every physics step and mostly hits the same support.
        public ICarrier CarrierOf(Collider support) {
            if (support == _carrierLookupCollider)
                return _carrierLookup;

            _carrierLookupCollider = support;
            _carrierLookup = support == null ? null : support.GetComponentInParent<ICarrier>();
            return _carrierLookup;
        }

        private static bool BelongsToSelf(RaycastHit hit, Transform self, Rigidbody selfBody) {
            if (hit.collider == null)
                return true;

            if (selfBody != null && hit.rigidbody == selfBody)
                return true;

            Transform hitTransform = hit.collider.transform;
            return hitTransform == self || hitTransform.IsChildOf(self);
        }
    }
}
