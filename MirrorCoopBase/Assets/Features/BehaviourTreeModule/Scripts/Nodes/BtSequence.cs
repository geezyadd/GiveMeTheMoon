namespace Features.BehaviourTreeModule.Nodes {
    // Runs its children in order and fails on the first failure. It remembers the running step, so steps that
    // already succeeded are not run again, but the pure checks before that step are re-checked every tick: a check
    // that stops holding fails the sequence and interrupts the running step.
    public sealed class BtSequence : BtComposite {
        private int _currentIndex;

        public BtSequence(string name) : base(name) { }

        public override bool CanEnter => CanEnterLeadingSteps();

        protected override void OnStart() =>
            _currentIndex = 0;

        protected override BtStatus OnRun(float deltaTime) {
            if (!DoPassedChecksHold(deltaTime))
                return BtStatus.Failure;

            for (; _currentIndex < Children.Count; _currentIndex++) {
                BtStatus result = Children[_currentIndex].Tick(deltaTime);
                if (result != BtStatus.Success)
                    return result;
            }

            return BtStatus.Success;
        }

        private bool DoPassedChecksHold(float deltaTime) {
            for (int i = 0; i < _currentIndex; i++) {
                BtNode child = Children[i];
                if (child.IsPureCheck && child.Tick(deltaTime) == BtStatus.Failure)
                    return false;
            }

            return true;
        }

        // The leading pure checks and the first real step decide whether the sequence fails at once.
        private bool CanEnterLeadingSteps() {
            for (int i = 0; i < Children.Count; i++) {
                BtNode child = Children[i];
                if (!child.CanEnter)
                    return false;

                if (!child.IsPureCheck)
                    return true;
            }

            return true;
        }
    }
}
