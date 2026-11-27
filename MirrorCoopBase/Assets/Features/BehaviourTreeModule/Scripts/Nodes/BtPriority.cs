namespace Features.BehaviourTreeModule.Nodes {
    // A reactive selector: every tick it tries its children from the first one and runs the first that does not fail.
    // A higher child that can enter interrupts the running lower one; the lower branch stops before the higher starts.
    public sealed class BtPriority : BtComposite {
        public BtPriority(string name) : base(name) { }

        public override bool CanEnter => HasEnterableChild();

        protected override BtStatus OnRun(float deltaTime) {
            for (int i = 0; i < Children.Count; i++) {
                BtNode child = Children[i];
                bool isRunning = child.Status == BtStatus.Running;
                if (!isRunning && !child.CanEnter)
                    continue;

                if (!isRunning)
                    StopRunningChildren();

                BtStatus result = child.Tick(deltaTime);
                if (result != BtStatus.Failure)
                    return result;
            }

            return BtStatus.Failure;
        }

        private bool HasEnterableChild() {
            for (int i = 0; i < Children.Count; i++) {
                if (Children[i].CanEnter)
                    return true;
            }

            return false;
        }
    }
}
