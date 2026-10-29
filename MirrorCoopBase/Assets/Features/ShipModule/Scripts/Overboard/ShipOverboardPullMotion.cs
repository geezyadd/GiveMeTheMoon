using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // The velocity of a body a rope pulls to the ship: the ship's own velocity, so the pull is in the ship's frame,
    // plus the pull speed toward the target, never past the target within one physics step.
    public sealed class ShipOverboardPullMotion {
        public Vector3 Step(Vector3 position, Vector3 target, Vector3 shipVelocity, float pullSpeed, float fixedDeltaTime) {
            Vector3 toTarget = target - position;
            float speed = Mathf.Min(pullSpeed, toTarget.magnitude / fixedDeltaTime);
            return shipVelocity + toTarget.normalized * speed;
        }
    }
}
