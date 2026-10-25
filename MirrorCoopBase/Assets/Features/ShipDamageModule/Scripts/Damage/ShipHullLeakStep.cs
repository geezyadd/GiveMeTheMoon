namespace Features.ShipDamageModule.Scripts.Damage {
    // One step of the breach leak: what is still gathered and what is dealt to the hull now (0 while below the step).
    public readonly struct ShipHullLeakStep {
        public float PendingLeak { get; }
        public float DealtLeak { get; }

        public ShipHullLeakStep(float pendingLeak, float dealtLeak) {
            PendingLeak = pendingLeak;
            DealtLeak = dealtLeak;
        }
    }
}
