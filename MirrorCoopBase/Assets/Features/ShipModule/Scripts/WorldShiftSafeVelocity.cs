using UnityEngine;

namespace Features.ShipModule.Scripts {
    // A velocity measured from frame-to-frame motion that ignores a jump no ship can fly: a world shift (an origin
    // recenter) moves everything hundreds of metres in one frame, and that is no velocity, so the last one is kept.
    public sealed class WorldShiftSafeVelocity {
        private const float MAX_SPEED_METERS_PER_SECOND = 120f;

        private Vector3 _lastPosition;
        private bool _hasLastPosition;

        public Vector3 Velocity { get; private set; }

        public void Accept(Vector3 displacement, float deltaTime) {
            if (deltaTime <= 0f)
                return;

            Vector3 velocity = displacement / deltaTime;
            if (velocity.sqrMagnitude <= MAX_SPEED_METERS_PER_SECOND * MAX_SPEED_METERS_PER_SECOND)
                Velocity = velocity;
        }

        // Follows one position over the frames; the first sample after a reset only starts the track.
        public void Track(Vector3 position, float deltaTime) {
            if (_hasLastPosition)
                Accept(position - _lastPosition, deltaTime);

            _lastPosition = position;
            _hasLastPosition = true;
        }

        public void Reset() =>
            _hasLastPosition = false;
    }
}
