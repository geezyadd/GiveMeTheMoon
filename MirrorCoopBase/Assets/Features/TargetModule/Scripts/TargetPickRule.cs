using System.Collections.Generic;
using UnityEngine;

namespace Features.TargetModule {
    // Which point a seeker goes for, pure.
    public static class TargetPickRule {
        // The nearest point in front of the seeker: within maxDistance of its origin and at most maxAngle degrees to
        // either side of its forward on the ground plane (a point right above or below is not ruled out by the angle).
        // -1 when there is none.
        public static int PickNearest(Vector3 origin, Vector3 forward, IReadOnlyList<Vector3> points, float maxDistance,
            float maxAngle) {
            Vector3 flatForward = new(forward.x, 0f, forward.z);
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++) {
                Vector3 offset = points[i] - origin;
                float distance = offset.magnitude;
                if (distance > maxDistance || distance >= bestDistance)
                    continue;

                Vector3 flat = new(offset.x, 0f, offset.z);
                if (flat.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f
                    && Vector3.Angle(flatForward, flat) > maxAngle)
                    continue;

                best = i;
                bestDistance = distance;
            }

            return best;
        }
    }
}
