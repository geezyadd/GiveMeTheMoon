using UnityEngine;

namespace Features.PlayerBodyModule {
    // The hit head's velocity change, pure.
    public static class HitPushRule {
        // The limb's velocity times speedScale, at least minSpeed: a slow limb still jerks the head, along its own
        // motion, or from the limb toward the head when it barely moves.
        public static Vector3 HeadVelocity(Vector3 limbVelocity, Vector3 limbToHead, float speedScale, float minSpeed) {
            Vector3 velocity = limbVelocity * speedScale;
            if (velocity.magnitude >= minSpeed)
                return velocity;

            Vector3 direction = velocity.sqrMagnitude > 0.0001f ? velocity : limbToHead;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized * minSpeed : Vector3.zero;
        }
    }
}
