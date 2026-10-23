namespace Features.FlightFieldModule.Scripts.Generation {
    public sealed class FieldRandomFactory : IFieldRandomFactory {
        public IFieldRandom Create(int seed) =>
            new FieldRandom(seed);
    }
}
