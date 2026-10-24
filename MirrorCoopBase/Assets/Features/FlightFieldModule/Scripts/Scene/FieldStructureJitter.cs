namespace Features.FlightFieldModule.Scripts.Scene {
    // The visual turn and scale of a built structure's trigger, drawn from the field seed and the placement index.
    public readonly struct FieldStructureJitter {
        public float YawDegrees { get; }
        public float Scale { get; }

        public FieldStructureJitter(float yawDegrees, float scale) {
            YawDegrees = yawDegrees;
            Scale = scale;
        }
    }
}
