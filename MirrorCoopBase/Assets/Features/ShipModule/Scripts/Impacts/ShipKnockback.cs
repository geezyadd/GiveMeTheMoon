using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // A velocity the flight adds to its own motion after a rock hit. A hit sets a target velocity that fades to zero
    // over the fade time (slowly at first, so the limited acceleration has time to turn the ship back, and to zero
    // without a step, so the cruise speed comes back without a jerk); the velocity follows the target with a limited
    // acceleration, which is all the deck and everyone on it feels.
    public sealed class ShipKnockback {
        private const float MIN_FADE_SECONDS = 0.05f;
        private const float MIN_ACCELERATION = 0.1f;
        private const float REST_SPEED_SQR = 0.0001f;

        private Vector3 _impulse;
        private Vector3 _velocity;
        private float _age = MIN_FADE_SECONDS;
        private float _fadeSeconds = MIN_FADE_SECONDS;
        private float _maxAcceleration = MIN_ACCELERATION;

        public Vector3 Velocity => _velocity;
        public Vector3 Target => _age >= _fadeSeconds ? Vector3.zero : _impulse * CalculateFade(_age / _fadeSeconds);
        public bool IsActive => _age < _fadeSeconds || _velocity.sqrMagnitude > REST_SPEED_SQR;

        // A new hit adds to what is left of the last one.
        public void Apply(Vector3 velocity, float fadeSeconds, float maxAcceleration) {
            _impulse = Target + velocity;
            _age = 0f;
            _fadeSeconds = Mathf.Max(MIN_FADE_SECONDS, fadeSeconds);
            _maxAcceleration = Mathf.Max(MIN_ACCELERATION, maxAcceleration);
        }

        // While the hull still touches a rock: the target keeps at least this speed along the direction and does
        // not fade, so the ship always gets out.
        public void Sustain(Vector3 direction, float minimumSpeed) {
            Vector3 target = Target;
            float along = Vector3.Dot(target, direction);
            if (along >= minimumSpeed)
                return;

            _impulse = target + direction * (minimumSpeed - along);
            _age = 0f;
        }

        // Advances by dt and returns the distance the knockback moves the ship in it.
        public Vector3 Step(float dt) {
            if (!IsActive || dt <= 0f)
                return Vector3.zero;

            _age += dt;
            _velocity = Vector3.MoveTowards(_velocity, Target, _maxAcceleration * dt);
            return _velocity * dt;
        }

        public void Reset() {
            _impulse = Vector3.zero;
            _velocity = Vector3.zero;
            _age = _fadeSeconds;
        }

        private static float CalculateFade(float progress) =>
            1f - progress * progress;
    }
}
