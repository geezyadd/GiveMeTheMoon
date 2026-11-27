using System;
using System.Globalization;

namespace Features.BehaviourTreeModule.Nodes {
    // After its child succeeds, fails for the given seconds of tree clock without ticking the child. The tree clock
    // runs whether or not this branch is ticked, so time spent in other branches counts too.
    public sealed class BtRecharge : BtDecorator {
        private const string NEGATIVE_DURATION = "A recharge time cannot be negative.";
        private const string REMAINING_FORMAT = "ready in {0:0.00} s";

        private readonly float _seconds;

        private double _readyAt;

        public BtRecharge(string name, float seconds) : base(name) {
            if (seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, NEGATIVE_DURATION);

            _seconds = seconds;
        }

        public bool IsReady => Clock.ElapsedSeconds >= _readyAt;
        public override bool CanEnter => IsReady && Child.CanEnter;

        public override string DebugText =>
            IsReady ? string.Empty : string.Format(CultureInfo.InvariantCulture, REMAINING_FORMAT, _readyAt - Clock.ElapsedSeconds);

        protected override BtStatus OnRun(float deltaTime) {
            if (!IsReady)
                return BtStatus.Failure;

            BtStatus result = Child.Tick(deltaTime);
            if (result == BtStatus.Success)
                _readyAt = Clock.ElapsedSeconds + _seconds;

            return result;
        }
    }
}
