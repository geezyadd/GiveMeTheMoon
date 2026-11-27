using System;
using System.Globalization;

namespace Features.BehaviourTreeModule.Nodes {
    // Runs for the given seconds of tree clock after it starts, then succeeds.
    public sealed class BtDelay : BtNode {
        private const string NEGATIVE_DURATION = "A delay cannot be negative.";
        private const string REMAINING_FORMAT = "{0:0.00} s left";

        private readonly float _seconds;

        private double _startedAt;

        public BtDelay(string name, float seconds) : base(name) {
            if (seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, NEGATIVE_DURATION);

            _seconds = seconds;
        }

        public override string DebugText =>
            Status == BtStatus.Running ? string.Format(CultureInfo.InvariantCulture, REMAINING_FORMAT, GetRemainingSeconds()) : string.Empty;

        protected override void OnStart() =>
            _startedAt = Clock.ElapsedSeconds;

        protected override BtStatus OnRun(float deltaTime) =>
            GetRemainingSeconds() > 0d ? BtStatus.Running : BtStatus.Success;

        private double GetRemainingSeconds() =>
            _startedAt + _seconds - Clock.ElapsedSeconds;
    }
}
