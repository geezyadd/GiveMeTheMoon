using UnityEngine;

namespace Features.AttackModule {
    // Whether an attack's limb is close enough to its target, pure.
    public static class AttackHitRule {
        // How far the target's point is from the limb's reach: the segment from the limb to reachAhead metres further
        // along its motion (just the limb when it barely moves). So the hit zone is a capsule stretched the way the limb
        // swings, and a limb about to pass the target hits it a little earlier instead of missing it.
        public static float DistanceToReach(Vector3 limb, Vector3 limbVelocity, Vector3 target, float reachAhead) {
            Vector3 toTarget = target - limb;
            if (reachAhead <= 0f || limbVelocity.sqrMagnitude < 0.0001f)
                return toTarget.magnitude;

            Vector3 direction = limbVelocity.normalized;
            float along = Mathf.Clamp(Vector3.Dot(toTarget, direction), 0f, reachAhead);
            return (toTarget - direction * along).magnitude;
        }
    }
}
