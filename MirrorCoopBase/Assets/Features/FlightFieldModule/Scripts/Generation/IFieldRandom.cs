namespace Features.FlightFieldModule.Scripts.Generation {
    public interface IFieldRandom {
        public int Range(int minInclusive, int maxExclusive);
        public float Value();
        // Starts the sequence of the given seed again, as a new instance with that seed would.
        public void Reseed(int seed);
    }
}
