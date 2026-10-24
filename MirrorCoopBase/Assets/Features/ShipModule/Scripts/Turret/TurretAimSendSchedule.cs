namespace Features.ShipModule.Scripts.Turret {
    // When the seated owner sends its aim: a trigger change at once, otherwise at most once per interval.
    public sealed class TurretAimSendSchedule {
        private readonly float _intervalSeconds;

        private bool _hasSent;
        private bool _sentTrigger;
        private double _nextSendTime;

        public TurretAimSendSchedule(float intervalSeconds) =>
            _intervalSeconds = intervalSeconds;

        public bool IsDue(bool isTriggerHeld, double nowSeconds) =>
            _hasSent == false || isTriggerHeld != _sentTrigger || nowSeconds >= _nextSendTime;

        public void MarkSent(bool isTriggerHeld, double nowSeconds) {
            _hasSent = true;
            _sentTrigger = isTriggerHeld;
            _nextSendTime = nowSeconds + _intervalSeconds;
        }

        public void Reset() =>
            _hasSent = false;
    }
}
