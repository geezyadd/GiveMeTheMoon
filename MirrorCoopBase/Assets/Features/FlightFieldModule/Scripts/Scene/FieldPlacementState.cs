namespace Features.FlightFieldModule.Scripts.Scene {
    // What this peer has built of one placement of the live field.
    public enum FieldPlacementState {
        None = 0,
        NotBuilt = 1,
        Built = 2,
        // A networked structure: the server spawns it through Mirror, this peer never builds it.
        NotLocal = 3,
    }
}
