using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // A camera shake that fades out: smooth noise per axis, scaled by the hit's strength. A weaker hit during a
    // stronger shake does not cut it short.
    public sealed class ShipCameraShake {
        private const float PITCH_SEED = 11.3f;
        private const float YAW_SEED = 47.9f;
        private const float ROLL_SEED = 83.1f;
        private const float OFFSET_X_SEED = 131.7f;
        private const float OFFSET_Y_SEED = 173.5f;

        private float _degrees;
        private float _meters;
        private float _seconds;
        private float _frequency;
        private float _age;

        public bool IsActive => _age < _seconds;

        private float Fade {
            get {
                float left = 1f - Mathf.Clamp01(_age / _seconds);
                return left * left;
            }
        }

        public void Start(ShipRockImpactFeel feel, float strength) {
            float degrees = feel.ShakeDegrees * strength;
            if (IsActive && degrees < _degrees * Fade)
                return;

            _degrees = degrees;
            _meters = feel.ShakeMeters * strength;
            _seconds = feel.ShakeSeconds;
            _frequency = feel.ShakeFrequency;
            _age = 0f;
        }

        public void Advance(float dt) =>
            _age += dt;

        public Quaternion SampleRotation() {
            float scale = _degrees * Fade;
            return Quaternion.Euler(Noise(PITCH_SEED) * scale, Noise(YAW_SEED) * scale, Noise(ROLL_SEED) * scale);
        }

        public Vector3 SampleOffset() {
            float scale = _meters * Fade;
            return new Vector3(Noise(OFFSET_X_SEED) * scale, Noise(OFFSET_Y_SEED) * scale, 0f);
        }

        private float Noise(float seed) =>
            Mathf.PerlinNoise(seed, _age * _frequency) * 2f - 1f;
    }
}
