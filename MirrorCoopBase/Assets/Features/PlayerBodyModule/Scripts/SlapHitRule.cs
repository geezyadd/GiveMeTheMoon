using UnityEngine;

namespace Features.PlayerBodyModule {
    // The slapped head's velocity change, pure.
    public static class SlapHitRule {
        // How far the head's centre is from the hand's reach: the segment from the hand to reachAhead metres further
        // along its motion (just the hand when it barely moves). So the hit zone is a capsule stretched the way the hand
        // swings, and a hand about to pass the head hits it a little earlier instead of missing it.
        public static float DistanceToReach(Vector3 hand, Vector3 handVelocity, Vector3 head, float reachAhead) {
            Vector3 toHead = head - hand;
            if (reachAhead <= 0f || handVelocity.sqrMagnitude < 0.0001f)
                return toHead.magnitude;

            Vector3 direction = handVelocity.normalized;
            float along = Mathf.Clamp(Vector3.Dot(toHead, direction), 0f, reachAhead);
            return (toHead - direction * along).magnitude;
        }

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
