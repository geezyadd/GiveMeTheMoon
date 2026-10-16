namespace Features.FloatingControllerModule {
    // The hover tuning of one floating body: the station float and the ship ride read the same values from it.
    public readonly struct HoverSpringSettings {
        // Support farther below the probe than this share of the clearance does not hold the body.
        private const float SUPPORT_REACH_FACTOR = 1.3f;

        public readonly float Clearance;
        public readonly float Stiffness;
        public readonly float Damping;
        public readonly float MaxAcceleration;
        public readonly float SkipIfFallingFasterThan;
        public readonly float SupportPause;

        public HoverSpringSettings(
            float clearance,
            float stiffness,
            float damping,
            float maxAcceleration,
            float skipIfFallingFasterThan,
            float supportPause) {
            Clearance = clearance;
            Stiffness = stiffness;
            Damping = damping;
            MaxAcceleration = maxAcceleration;
            SkipIfFallingFasterThan = skipIfFallingFasterThan;
            SupportPause = supportPause;
        }

        public float SupportReach => Clearance * SUPPORT_REACH_FACTOR;
    }
}
