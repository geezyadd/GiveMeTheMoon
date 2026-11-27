using System;

namespace Features.BehaviourTreeModule.Nodes {
    // An inline leaf: the delegate gets the tick's delta time and returns Running, Success or Failure.
    public sealed class BtAction : BtNode {
        private readonly Func<float, BtStatus> _run;

        public BtAction(string name, Func<float, BtStatus> run) : base(name) =>
            _run = run;

        protected override BtStatus OnRun(float deltaTime) =>
            _run(deltaTime);
    }
}
