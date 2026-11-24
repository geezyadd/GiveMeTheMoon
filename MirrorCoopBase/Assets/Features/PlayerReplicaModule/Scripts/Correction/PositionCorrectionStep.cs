using UnityEngine;

namespace Features.PlayerReplicaModule.Correction {
    // What one physics step of correction does to a copy: move it by Offset, or, when IsSnap, put it at the target
    // (Offset is then the whole error).
    public readonly struct PositionCorrectionStep {
        public readonly Vector3 Offset;
        public readonly bool IsSnap;

        private PositionCorrectionStep(Vector3 offset, bool isSnap) {
            Offset = offset;
            IsSnap = isSnap;
        }

        public static PositionCorrectionStep Move(Vector3 offset) =>
            new(offset, false);

        public static PositionCorrectionStep Snap(Vector3 error) =>
            new(error, true);
    }
}
