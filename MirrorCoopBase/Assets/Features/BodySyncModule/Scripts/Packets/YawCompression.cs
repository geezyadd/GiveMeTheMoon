using UnityEngine;

namespace Features.BodySyncModule.Packets {
    // Packs a look angle into two bytes: the full turn split into 65 536 steps, about 0.0055 degrees each.
    public static class YawCompression {
        private const float FULL_TURN_DEGREES = 360f;
        private const int STEPS = ushort.MaxValue + 1;

        public static ushort Pack(float yawDegrees) {
            float turnShare = Mathf.Repeat(yawDegrees, FULL_TURN_DEGREES) / FULL_TURN_DEGREES;
            return (ushort)(Mathf.RoundToInt(turnShare * STEPS) % STEPS);
        }

        // Returns the angle in [0, 360).
        public static float Unpack(ushort packedYaw) =>
            packedYaw * FULL_TURN_DEGREES / STEPS;
    }
}
