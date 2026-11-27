using System;

namespace Features.BehaviourTreeModule.Nodes {
    // Runs its child only while the predicate holds; once it stops holding the child is interrupted and the guard
    // fails. The predicate must have no side effects.
    public sealed class BtGuard : BtDecorator {
        private readonly Func<bool> _holds;

        public BtGuard(string name, Func<bool> holds) : base(name) =>
            _holds = holds;

        public override bool CanEnter => _holds() && Child.CanEnter;

        protected override BtStatus OnRun(float deltaTime) =>
            _holds() ? Child.Tick(deltaTime) : BtStatus.Failure;
    }
}
