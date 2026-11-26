namespace Features.ShipModule.Scripts.Overboard {
    // What the server does with one player this tick.
    public enum ShipOverboardDecision {
        None = 0,
        MarkInside = 1,
        GoOverboard = 2,
        EndDrift = 3,
        // In flight: the void takes the player (a death).
        Expire = 4,
        // On the ground: the player is put back at the area's return point.
        ReturnToArea = 5,
    }
}
