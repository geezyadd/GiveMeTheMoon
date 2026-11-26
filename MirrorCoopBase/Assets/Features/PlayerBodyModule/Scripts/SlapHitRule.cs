using UnityEngine;

namespace Features.PlayerBodyModule {
    // The slapped head's velocity change, pure.
    public static class SlapHitRule {
        // The hand's velocity times speedScale, at least minSpeed: a slow hand still jerks the head, along its own
        // motion, or from the hand toward the head when it barely moves.
        public static Vector3 HeadVelocity(Vector3 handVelocity, Vector3 handToHead, float speedScale, float minSpeed) {
            Vector3 velocity = handVelocity * speedScale;
            if (velocity.magnitude >= minSpeed)
                return velocity;

            Vector3 direction = velocity.sqrMagnitude > 0.0001f ? velocity : handToHead;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized * minSpeed : Vector3.zero;
        }
    }
}
