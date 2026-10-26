using Features.ShipModule.Scripts.Hull;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // A hull box swept from the ship's last checked pose to its pose now, as one box turned like the ship now, so a
    // fast ship cannot pass a rock between two checks. The turn between two checks is small and left out.
    public readonly struct ShipHullSweep {
        public Vector3 Center { get; }
        public Vector3 HalfExtents { get; }
        public Quaternion Rotation { get; }

        private ShipHullSweep(Vector3 center, Vector3 halfExtents, Quaternion rotation) {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = rotation;
        }

        public static ShipHullSweep Create(ShipHullBox box, Pose from, Pose to) {
            Vector3 start = from.position + from.rotation * box.Center;
            Vector3 end = to.position + to.rotation * box.Center;
            Vector3 travel = Quaternion.Inverse(to.rotation) * (end - start);
            Vector3 halfTravel = new(Mathf.Abs(travel.x) * 0.5f, Mathf.Abs(travel.y) * 0.5f, Mathf.Abs(travel.z) * 0.5f);
            return new ShipHullSweep((start + end) * 0.5f, box.HalfExtents + halfTravel, to.rotation);
        }

        public ShipHullSweep Grow(float margin) =>
            new(Center, HalfExtents + Vector3.one * margin, Rotation);

        public Vector3 ClosestPoint(Vector3 point) {
            Vector3 local = Quaternion.Inverse(Rotation) * (point - Center);
            Vector3 clamped = new(Mathf.Clamp(local.x, -HalfExtents.x, HalfExtents.x), Mathf.Clamp(local.y, -HalfExtents.y, HalfExtents.y),
                Mathf.Clamp(local.z, -HalfExtents.z, HalfExtents.z));
            return Center + Rotation * clamped;
        }

        public bool Touches(Vector3 sphereCenter, float sphereRadius) =>
            (ClosestPoint(sphereCenter) - sphereCenter).sqrMagnitude <= sphereRadius * sphereRadius;
    }
}
