namespace Features.ShipModule.Scripts {
    public readonly struct ShipSocketRunState {
        public int LoopIndex { get; }
        public ShipRunPhase Phase { get; }

        public ShipSocketRunState(int loopIndex, ShipRunPhase phase) {
            LoopIndex = loopIndex;
            Phase = phase;
        }
    }
}
