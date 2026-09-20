namespace Features.ShipModule.Scripts {
    public sealed class ShipRunModel {
        public ShipRunPhase Phase { get; internal set; } = ShipRunPhase.Build;
        public int LoopIndex { get; internal set; }
        public double CruiseEndNetworkTime { get; internal set; }
        public bool LaunchLocked { get; internal set; }
        public ShipRunAbortReason LastAbortReason { get; internal set; }

        internal void ResetMatch() {
            Phase = ShipRunPhase.Build;
            LoopIndex = 0;
            CruiseEndNetworkTime = 0d;
            LaunchLocked = false;
            LastAbortReason = ShipRunAbortReason.None;
        }
    }
}
