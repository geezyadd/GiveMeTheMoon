namespace Features.InventoryModule.Scripts {
    // The release button as a tap or a hold: let go before the hold time it is a tap (drop), held to the hold time it
    // is a hold (throw) at once, and its later release does nothing.
    public sealed class ReleasePress {
        private readonly float _holdSeconds;
        private float _pressTime;
        private bool _isPressed;

        public ReleasePress(float holdSeconds) =>
            _holdSeconds = holdSeconds;

        public bool IsPressed => _isPressed;

        public void Press(float now) {
            _isPressed = true;
            _pressTime = now;
        }

        public bool TryCompleteHold(float now) {
            if (_isPressed == false || now - _pressTime < _holdSeconds)
                return false;

            _isPressed = false;
            return true;
        }

        public bool TryEndAsTap() {
            if (_isPressed == false)
                return false;

            _isPressed = false;
            return true;
        }

        public void Cancel() =>
            _isPressed = false;
    }
}
