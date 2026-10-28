namespace Features.FlightFieldModule.Contracts {
    // Server: the one way a piece of ore reaches the ship (it entered the hull, a rope pulled it in). The piece is
    // gone on every peer and IFieldOreEvents.OreCollected fires once, however often it is asked.
    public interface IFieldOreCollector {
        public FieldOreCollectResult ServerCollect(FieldOreId id);
    }
}
