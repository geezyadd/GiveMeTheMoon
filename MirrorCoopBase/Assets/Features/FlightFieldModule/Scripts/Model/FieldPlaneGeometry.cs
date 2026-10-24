using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // Plane geometry shared by the generation steps and the field tools: a world point on the ground plane (x, z) and
    // the distance from a point to a segment (a zero-length segment is its start point).
    public static class FieldPlaneGeometry {
        public static Vector2 ToGround(Vector3 point) =>
            new(point.x, point.z);

        public static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end) {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float along = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
            return Vector2.Distance(point, start + segment * along);
        }
    }
}
