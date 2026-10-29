namespace Features.ShipModule.Scripts.Overboard {
    // What the server does with one player this tick.
    public enum ShipOverboardDecision {
        None = 0,
        MarkRidden = 1,
        ForgetRide = 2,
        GoOverboard = 3,
        EndOnDeck = 4,
        Capture = 5,
        Expire = 6,
    }
}
