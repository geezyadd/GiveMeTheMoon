namespace Features.FlightFieldModule.Contracts {
    public enum FieldRockBreakResult {
        None = 0,
        Broken = 1,
        // No field stands on the server (or this peer is not the server).
        NoField = 2,
        NotFound = 3,
        AlreadyBroken = 4,
        Unbreakable = 5,
    }
}
