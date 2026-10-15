namespace Features.FlightFieldModule.Scripts.Generation {
    public interface IFieldRandomFactory {
        public IFieldRandom Create(int seed);
    }
}
