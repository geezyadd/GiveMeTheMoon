using UnityEngine;

namespace Features.TurretModule.Scripts {
    // Eases another player's received turret aim towards each new value, so throttled updates read as smooth turns.
    // The yaw takes the short way round.
    public sealed class TurretAimSmoothing {
        private readonly float _smoothingSeconds;

        private bool _hasValue;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public TurretAimSmoothing(float smoothingSeconds) =>
            _smoothingSeconds = smoothingSeconds;

        public void Advance(float targetYaw, float targetPitch, float deltaTime) {
            if (_hasValue == false || _smoothingSeconds <= 0f) {
                SnapTo(targetYaw, targetPitch);
                return;
            }

            float share = 1f - Mathf.Exp(-deltaTime / _smoothingSeconds);
            Yaw += Mathf.DeltaAngle(Yaw, targetYaw) * share;
            Pitch += (targetPitch - Pitch) * share;
        }

        public void SnapTo(float yaw, float pitch) {
            Yaw = yaw;
            Pitch = pitch;
            _hasValue = true;
        }

        public void Reset() =>
            _hasValue = false;
    }
}
