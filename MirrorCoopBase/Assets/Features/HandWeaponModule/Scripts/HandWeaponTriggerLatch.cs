namespace Features.HandWeaponModule.Scripts {
    // The trigger the owner reports for a weapon in the hand: a press stays reported for at least the minimum time,
    // even when the button is let go sooner, so a short click survives the network's send interval and every peer
    // replays the shot. A held button stays reported while it is held.
    public sealed class HandWeaponTriggerLatch {
        private readonly float _minReportSeconds;

        private bool _wasPressed;
        private float _reportedSeconds;

        public bool IsReported { get; private set; }

        public HandWeaponTriggerLatch(float minReportSeconds) =>
            _minReportSeconds = minReportSeconds;

        public bool Advance(bool isPressed, float deltaTime) {
            bool isNewPress = isPressed && _wasPressed == false;
            _wasPressed = isPressed;
            if (isNewPress && IsReported == false) {
                IsReported = true;
                _reportedSeconds = 0f;
                return true;
            }

            if (IsReported)
                _reportedSeconds += deltaTime;

            IsReported = IsReported && (isPressed || _reportedSeconds < _minReportSeconds);
            return IsReported;
        }
    }
}
