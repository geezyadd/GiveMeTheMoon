using UnityEngine;

namespace Features.ActiveRagdollModule {
    // One place the body might be standing on.
    public readonly struct GroundSample {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Collider Collider;

        public GroundSample(Vector3 point, Vector3 normal, Collider collider) {
            Point = point;
            Normal = normal;
            Collider = collider;
        }
    }
}
