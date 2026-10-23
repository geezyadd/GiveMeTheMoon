namespace Features.FlightFieldModule.Scripts.Generation {
    // An option picked by its relative weight among its siblings.
    public interface IFieldWeighted {
        public float Weight { get; }
    }
}
