using System;

namespace Features.BehaviourTreeModule.Nodes {
    // Succeeds when its predicate holds, fails otherwise, in the tick it starts. The predicate must have no side
    // effects: sequences and priorities ask it again every tick.
    public sealed class BtCheck : BtNode {
        private readonly Func<bool> _holds;

        public BtCheck(string name, Func<bool> holds) : base(name) =>
            _holds = holds;

        public override bool CanEnter => _holds();
        public override bool IsPureCheck => true;

        protected override BtStatus OnRun(float deltaTime) =>
            _holds() ? BtStatus.Success : BtStatus.Failure;
    }
}
