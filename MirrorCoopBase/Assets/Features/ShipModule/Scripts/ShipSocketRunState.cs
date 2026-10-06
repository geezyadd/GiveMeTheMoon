namespace Features.ShipModule.Scripts {
    public readonly struct ShipSocketRunState {
        public static readonly ShipSocketRunState NoRun = new(false, 0, ShipRunPhase.Build);

        public bool HasRun { get; }
        public int LoopIndex { get; }
        public ShipRunPhase Phase { get; }

        public ShipSocketRunState(bool hasRun, int loopIndex, ShipRunPhase phase) {
            HasRun = hasRun;
            LoopIndex = loopIndex;
            Phase = phase;
        }
    }
}
