namespace Features.FlightFieldModule.Scripts.Ore {
    public enum FieldOreHookResult {
        None = 0,
        Hooked = 1,
        // No field stands on the server (or this peer is not the server).
        NoField = 2,
        NotFound = 3,
        AlreadyHooked = 4,
        // The reported hit lies too far from where the piece is on the server.
        TooFar = 5,
        // No ship to pull the piece to.
        NoShip = 6,
    }
}
