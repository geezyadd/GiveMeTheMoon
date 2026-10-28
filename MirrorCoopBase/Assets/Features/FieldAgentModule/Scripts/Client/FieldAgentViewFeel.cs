using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // The local look on top of the synced pose: bank into turns, pitch with forward acceleration, a light bob. It is
    // worked out from how the shown pose moves from frame to frame, so nothing of it goes over the network.
    public sealed class FieldAgentViewFeel {
        private const float MIN_DELTA_TIME = 0.0001f;

        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;
        private float _lastYaw;
        private float _bank;
        private float _tilt;
        private bool _hasLast;

        public float Bank => _bank;
        public float Tilt => _tilt;

        public void Reset() {
            _hasLast = false;
            _bank = 0f;
            _tilt = 0f;
            _lastVelocity = Vector3.zero;
        }

        public void Update(Vector3 position, Quaternion rotation, float deltaTime, FieldAgentProfile profile) {
            float yaw = rotation.eulerAngles.y;
            if (_hasLast == false || deltaTime < MIN_DELTA_TIME) {
                Remember(position, Vector3.zero, yaw);
                return;
            }

            Vector3 velocity = (position - _lastPosition) / deltaTime;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / deltaTime;
            float forwardAcceleration = Vector3.Dot((velocity - _lastVelocity) / deltaTime, rotation * Vector3.forward);
            float targetBank = Mathf.Clamp(-yawRate * profile.BankPerTurnRate, -profile.MaxBankDegrees, profile.MaxBankDegrees);
            float targetTilt = Mathf.Clamp(forwardAcceleration * profile.TiltPerAcceleration, -profile.MaxTiltDegrees, profile.MaxTiltDegrees);
            float follow = 1f - Mathf.Exp(-deltaTime / Mathf.Max(profile.FeelSmoothSeconds, MIN_DELTA_TIME));
            _bank = Mathf.Lerp(_bank, targetBank, follow);
            _tilt = Mathf.Lerp(_tilt, targetTilt, follow);
            Remember(position, velocity, yaw);
        }

        public float CalculateBob(float time, float phase, FieldAgentProfile profile) =>
            Mathf.Sin((time * profile.BobFrequency + phase) * Mathf.PI * 2f) * profile.BobAmplitude;

        private void Remember(Vector3 position, Vector3 velocity, float yaw) {
            _lastPosition = position;
            _lastVelocity = velocity;
            _lastYaw = yaw;
            _hasLast = true;
        }
    }
}
