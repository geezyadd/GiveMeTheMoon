using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunModel {
        public ShipRunPhase Phase { get; internal set; } = ShipRunPhase.Build;
        public int LoopIndex { get; internal set; }
        public double CruiseEndNetworkTime { get; internal set; }
        public bool LaunchLocked { get; internal set; }
        public ShipRunAbortReason LastAbortReason { get; internal set; }
        public float TransitWorkRemaining { get; internal set; }
        public float TransitSpeed { get; internal set; } = 1f;
        public float TransitAlignment { get; internal set; } = 1f;
        public Vector3 TransitDestination { get; internal set; }
        public float TransitSecondsRemaining { get; internal set; }

        internal void ResetMatch() {
            Phase = ShipRunPhase.Build;
            LoopIndex = 0;
            CruiseEndNetworkTime = 0d;
            LaunchLocked = false;
            LastAbortReason = ShipRunAbortReason.None;
            TransitWorkRemaining = 0f;
            TransitSpeed = 1f;
            TransitAlignment = 1f;
            TransitDestination = Vector3.zero;
            TransitSecondsRemaining = 0f;
        }
    }
}
