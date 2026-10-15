namespace Features.FlightFieldModule.Scripts.Generation {
    public interface IFieldRandom {
        public int Range(int minInclusive, int maxExclusive);
        public float Value();
    }
}
