using UnityEngine;

namespace Features.ExplosionModule {
    // Whether an explosion went off on the ground: something solid right under its centre (it landed there, not in the
    // air), facing up, and a wide enough flat piece around it: the surface all around, at the flat radius, is about as
    // high as under the centre (a rock's tilted facets there do not matter). Near an edge, on a slope or over a small thing it is
    // not: the ground effect would hang over the void or cut into the slope, so the air one plays instead.
    public sealed class ExplosionGroundProbe {
        private const int RING_RAYS = 8;
        // Rays start this far above the surface they look for, so a centre right on the surface is still found.
        private const float START_ABOVE_METERS = 0.5f;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        // On the ground: the surface point under the centre, where the ground effect plays.
        public bool TryFindGround(Vector3 center, float flatRadius, in ExplosionGroundSettings settings, out Vector3 groundPoint) {
            groundPoint = center;
            float minUp = Mathf.Cos(settings.MaxSlopeDegrees * Mathf.Deg2Rad);
            Vector3 start = center + Vector3.up * START_ABOVE_METERS;
            if (TryCastDown(start, START_ABOVE_METERS + settings.ProbeDistance, settings.Layers, out RaycastHit under) == false
                || under.normal.y < minUp)
                return false;

            float reach = START_ABOVE_METERS + settings.FlatTolerance * 2f;
            for (int i = 0; i < RING_RAYS; i++) {
                float angle = i * Mathf.PI * 2f / RING_RAYS;
                Vector3 ringStart = under.point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * flatRadius
                    + Vector3.up * (START_ABOVE_METERS + settings.FlatTolerance);
                if (TryCastDown(ringStart, reach, settings.Layers, out RaycastHit ring) == false
                    || Mathf.Abs(ring.point.y - under.point.y) > settings.FlatTolerance)
                    return false;
            }

            groundPoint = under.point;
            return true;
        }

        // The nearest solid hit (triggers ignored).
        private bool TryCastDown(Vector3 start, float distance, LayerMask layers, out RaycastHit nearest) {
            nearest = default;
            int count = Physics.RaycastNonAlloc(start, Vector3.down, _hits, distance, layers, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++) {
                if (_hits[i].distance >= best)
                    continue;

                best = _hits[i].distance;
                nearest = _hits[i];
            }

            return count > 0;
        }
    }
}
