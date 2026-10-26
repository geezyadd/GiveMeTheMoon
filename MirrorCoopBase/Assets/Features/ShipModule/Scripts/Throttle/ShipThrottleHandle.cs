using UnityEngine;

namespace Features.ShipModule.Scripts.Throttle {
    // The handle under the holder's hand: the mouse moves the hand, the heavy handle follows it with inertia and a
    // top speed, the detents pull it in, and it reports a click when it comes into a detent.
    public sealed class ShipThrottleHandle {
        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;
        private readonly IShipThrottleDetentRule _shipThrottleDetentRule;

        private float _handTarget;
        private float _velocity;
        private int _clickedDetent = ShipThrottleDetentRule.NO_DETENT;

        public float Position { get; private set; }

        public ShipThrottleHandle(ShipThrottleConfiguration shipThrottleConfiguration, IShipThrottleDetentRule shipThrottleDetentRule) {
            _shipThrottleConfiguration = shipThrottleConfiguration;
            _shipThrottleDetentRule = shipThrottleDetentRule;
        }

        public void Begin(float position) {
            Position = position;
            _handTarget = position;
            _velocity = 0f;
            _clickedDetent = _shipThrottleDetentRule.FindClickedDetent(position);
        }

        public void Release(float maxPosition) {
            Position = Mathf.Min(_shipThrottleDetentRule.SettleOnRelease(Position), maxPosition);
            _velocity = 0f;
        }

        // Returns true when the handle clicked into a detent this step. The handle never goes past the max position.
        public bool Step(float lookForward, float deltaTime, float maxPosition) {
            if (deltaTime <= 0f)
                return false;

            _handTarget = Mathf.Clamp(_handTarget + lookForward * _shipThrottleConfiguration.ThrottlePerLookUnit, 0f, maxPosition);
            float settle = Mathf.Min(_shipThrottleDetentRule.PullTowardsDetent(_handTarget), maxPosition);
            Position = Mathf.SmoothDamp(
                Position,
                settle,
                ref _velocity,
                _shipThrottleConfiguration.HandleSmoothSeconds,
                _shipThrottleConfiguration.HandleMaxSpeed,
                deltaTime);
            Position = Mathf.Clamp(Position, 0f, maxPosition);
            return UpdateClick();
        }

        private bool UpdateClick() {
            int detent = _shipThrottleDetentRule.FindClickedDetent(Position);
            bool isNewDetent = detent != ShipThrottleDetentRule.NO_DETENT && detent != _clickedDetent;
            _clickedDetent = detent;
            return isNewDetent;
        }
    }
}
