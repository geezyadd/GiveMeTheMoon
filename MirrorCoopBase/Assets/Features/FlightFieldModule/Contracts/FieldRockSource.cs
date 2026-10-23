namespace Features.FlightFieldModule.Contracts {
    // Which part of the field a rock belongs to: a cluster of a placed structure or the debris of a passable cell.
    public enum FieldRockSource {
        None = 0,
        Cluster = 1,
        Debris = 2,
    }
}
