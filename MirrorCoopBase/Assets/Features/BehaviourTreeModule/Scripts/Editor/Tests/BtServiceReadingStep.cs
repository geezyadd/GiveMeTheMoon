using Zenject;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtServiceReadingStep : BtNode {
        private const string NAME = "Reads service";

        public BtServiceReadingStep() : base(NAME) { }

        public BtTestService Service { get; private set; }

        [Inject]
        private void InjectDependencies(BtTestService service) =>
            Service = service;

        protected override BtStatus OnRun(float deltaTime) =>
            BtStatus.Success;
    }
}
