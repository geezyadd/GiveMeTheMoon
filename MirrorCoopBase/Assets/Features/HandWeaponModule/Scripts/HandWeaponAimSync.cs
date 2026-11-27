using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // How a weapon in the hand shares where it points: the holder sends its aim when it turned far enough and no more
    // often than the interval; every other peer turns its view of the weapon toward the received aim over the
    // smoothing time, so a new value never snaps it. Pure: the caller gives the time.
    public sealed class HandWeaponAimSync {
        private readonly float _sendIntervalSeconds;
        private readonly float _minSendDegrees;
        private readonly float _smoothingSeconds;

        private Vector3 _sentAim = Vector3.forward;
        private double _nextSendTime;

        public HandWeaponAimSync(float sendIntervalSeconds, float minSendDegrees, float smoothingSeconds) {
            _sendIntervalSeconds = sendIntervalSeconds;
            _minSendDegrees = minSendDegrees;
            _smoothingSeconds = smoothingSeconds;
        }

        public bool IsSendDue(Vector3 aim, double now) =>
            now >= _nextSendTime && Vector3.Angle(_sentAim, aim) >= _minSendDegrees;

        public void MarkSent(Vector3 aim, double now) {
            _sentAim = aim;
            _nextSendTime = now + _sendIntervalSeconds;
        }

        // An empty received aim (not synced yet) keeps the shown one.
        public Vector3 Smooth(Vector3 shown, Vector3 received, float deltaTime) {
            if (received.sqrMagnitude < 1e-6f)
                return shown;

            if (_smoothingSeconds <= 0f || shown.sqrMagnitude < 1e-6f)
                return received.normalized;

            float share = 1f - Mathf.Exp(-deltaTime / _smoothingSeconds);
            return Vector3.Slerp(shown.normalized, received.normalized, share);
        }
    }
}
