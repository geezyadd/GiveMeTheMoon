using Features.GrabModule.Scripts.Held;

namespace Features.ShipModule.Scripts.Throttle {
    // One frame of the local hold: what to tell the interaction (send, lost) and whether the handle clicked into a detent.
    public readonly struct ShipThrottleLocalStep {
        public HeldControlFrame Frame { get; }
        public bool IsDetentClick { get; }

        public ShipThrottleLocalStep(HeldControlFrame frame, bool isDetentClick) {
            Frame = frame;
            IsDetentClick = isDetentClick;
        }
    }
}
