using UnityEngine;

namespace Features.PlayerReplicaModule.Correction {
    // Where a copy should be now: the packet shows its owner as they were when the snapshot was taken, so the position
    // is moved ahead along the packet's velocity by the packet's age. The age is capped, so a copy that hears nothing
    // for a while does not run off, and a negative age (clock estimate error) counts as zero.
    public static class CorrectionTarget {
        public static Vector3 Predict(Vector3 packetPosition, Vector3 packetVelocity, double packetAgeSeconds, float leadLimitSeconds) {
            float age = Mathf.Clamp((float)packetAgeSeconds, 0f, leadLimitSeconds);
            return packetPosition + packetVelocity * age;
        }
    }
}
