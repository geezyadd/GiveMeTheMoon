using Features.GrabModule.Scripts.Held;
using UnityEngine;

namespace Features.ShipModule.Scripts.Throttle {
    // The holder's peer: moves the handle at once (no wait for the server), sends its position at most every send
    // interval, and ends the hold when the server gave the handle to someone else or never confirmed the take.
    public sealed class ShipThrottleLocalHold {
        private const float SENT_POSITION_EPSILON = 0.0005f;

        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;
        private readonly ShipThrottleHandle _shipThrottleHandle;

        private string _playerKey;
        private bool _isConfirmed;
        private float _heldSeconds;
        private float _secondsSinceSend;
        private float _sentPosition;

        public bool IsHolding { get; private set; }
        public float Position => _shipThrottleHandle.Position;

        public ShipThrottleLocalHold(ShipThrottleConfiguration shipThrottleConfiguration, ShipThrottleHandle shipThrottleHandle) {
            _shipThrottleConfiguration = shipThrottleConfiguration;
            _shipThrottleHandle = shipThrottleHandle;
        }

        public void Begin(string playerKey, float position) {
            _playerKey = playerKey;
            _isConfirmed = false;
            _heldSeconds = 0f;
            _secondsSinceSend = 0f;
            _sentPosition = position;
            _shipThrottleHandle.Begin(position);
            IsHolding = true;
        }

        public ShipThrottleLocalStep Drive(float lookForward, float deltaTime, float maxPosition, string holderKey) {
            if (IsHolding == false)
                return new ShipThrottleLocalStep(new HeldControlFrame(true, false, Position), false);

            _heldSeconds += deltaTime;
            _secondsSinceSend += deltaTime;
            ConfirmTake(holderKey);
            if (IsLost(holderKey))
                return new ShipThrottleLocalStep(new HeldControlFrame(true, false, Position), false);

            bool isClick = _shipThrottleHandle.Step(lookForward, deltaTime, maxPosition);
            bool isSendDue = HasUnsentPosition() && _secondsSinceSend >= _shipThrottleConfiguration.SendIntervalSeconds;
            if (isSendDue)
                MarkSent();

            return new ShipThrottleLocalStep(new HeldControlFrame(false, isSendDue, Position), isClick);
        }

        public HeldControlFrame End(float maxPosition) {
            if (IsHolding)
                _shipThrottleHandle.Release(maxPosition);

            bool hasPositionToSend = IsHolding && HasUnsentPosition();
            IsHolding = false;
            if (hasPositionToSend)
                MarkSent();

            return new HeldControlFrame(false, hasPositionToSend, Position);
        }

        private void ConfirmTake(string holderKey) {
            if (holderKey == _playerKey)
                _isConfirmed = true;
        }

        // Another holder, a let go after the server confirmed the take, or a take it never confirmed in time.
        private bool IsLost(string holderKey) {
            if (holderKey == _playerKey)
                return false;

            return string.IsNullOrEmpty(holderKey) == false || _isConfirmed || _heldSeconds > _shipThrottleConfiguration.TakeTimeoutSeconds;
        }

        private bool HasUnsentPosition() =>
            Mathf.Abs(Position - _sentPosition) > SENT_POSITION_EPSILON;

        private void MarkSent() {
            _sentPosition = Position;
            _secondsSinceSend = 0f;
        }
    }
}
