namespace Features.PointOfInterestModule.Scripts.Network {
    public enum PointOfInterestState {
        None = 0,
        // Waiting for the ship to enter its zone.
        Dormant = 1,
        // The ship entered the zone once and its content started; the reward container still stands.
        Triggered = 2,
        // The crew opened the reward container (shot it open or rammed it) and was paid.
        Opened = 3,
    }
}
