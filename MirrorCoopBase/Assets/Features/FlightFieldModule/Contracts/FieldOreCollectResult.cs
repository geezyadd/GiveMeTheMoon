namespace Features.FlightFieldModule.Contracts {
    public enum FieldOreCollectResult {
        None = 0,
        Collected = 1,
        // No field stands on the server (or this peer is not the server).
        NoField = 2,
        NotFound = 3,
    }
}
