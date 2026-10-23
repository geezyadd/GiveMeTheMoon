namespace Features.FlightFieldModule.Scripts.Generation.Noise {
    public interface IFieldNoise {
        // A smooth value in 0..1; neighbouring points give close values, one unit is about one feature.
        public float Sample(int seed, float x, float y);
    }
}
