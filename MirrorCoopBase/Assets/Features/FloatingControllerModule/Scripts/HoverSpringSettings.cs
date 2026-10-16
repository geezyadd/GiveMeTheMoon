namespace Features.FloatingControllerModule {
    // The hover tuning of one floating body: the station float and the ship ride read the same values from it.
    public readonly struct HoverSpringSettings {
        public readonly float Clearance;
        public readonly float Stiffness;
        public readonly float Damping;
        public readonly float MaxAcceleration;
        public readonly float SkipIfFallingFasterThan;
        public readonly float SupportPause;
        public readonly float SupportReachFactor;

        // Support farther below the probe than this does not hold the body.
        public float SupportReach => Clearance * SupportReachFactor;

        public HoverSpringSettings(
            float clearance,
            float stiffness,
            float damping,
            float maxAcceleration,
            float skipIfFallingFasterThan,
            float supportPause,
            float supportReachFactor) {
            Clearance = clearance;
            Stiffness = stiffness;
            Damping = damping;
            MaxAcceleration = maxAcceleration;
            SkipIfFallingFasterThan = skipIfFallingFasterThan;
            SupportPause = supportPause;
            SupportReachFactor = supportReachFactor;
        }
    }
}
