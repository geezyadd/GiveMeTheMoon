namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtServiceTestTree : BtTree {
        public BtServiceTestTree(BtTestService service) =>
            Service = service;

        public BtTestService Service { get; }
        public BtServiceReadingStep Step { get; } = new();

        protected override BtNode Describe(BtBuilder builder) =>
            builder
                .Sequence("Root")
                    .Add(Step)
                .End()
                .Build();
    }
}
