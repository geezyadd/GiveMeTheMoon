using System;

namespace Features.BehaviourTreeModule.Nodes {
    // Turns its child's Success into Failure and back; Running passes through.
    public sealed class BtNot : BtDecorator {
        private const string UNEXPECTED_STATUS = "A ticked node cannot be Inactive.";

        public BtNot(string name) : base(name) { }

        public override bool CanEnter => !Child.IsPureCheck || !Child.CanEnter;
        public override bool IsPureCheck => Child.IsPureCheck;

        protected override BtStatus OnRun(float deltaTime) =>
            Flip(Child.Tick(deltaTime));

        private static BtStatus Flip(BtStatus status) =>
            status switch {
                BtStatus.Success => BtStatus.Failure,
                BtStatus.Failure => BtStatus.Success,
                BtStatus.Running => BtStatus.Running,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, UNEXPECTED_STATUS)
            };
    }
}
