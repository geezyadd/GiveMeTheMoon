using UnityEngine;

namespace Features.CameraModule.Scripts.Shake {
    // How far the shakes turn (Euler degrees) and move (metres) the view this frame, in the look pivot's own space.
    public readonly struct CameraShakePose {
        public CameraShakePose(Vector3 eulerDegrees, Vector3 offset) {
            EulerDegrees = eulerDegrees;
            Offset = offset;
        }

        public Vector3 EulerDegrees { get; }
        public Vector3 Offset { get; }

        public CameraShakePose Add(in CameraShakePose other) =>
            new(EulerDegrees + other.EulerDegrees, Offset + other.Offset);
    }
}
