namespace Features.FlightFieldModule.Scripts.Model {
    // Main: a guaranteed passage to B. Alternative: branches off a main route and rejoins it, may be closed.
    // DeadEnd: branches off a main route and ends in a ridge.
    public enum FieldRouteKind {
        None = 0,
        Main = 1,
        Alternative = 2,
        DeadEnd = 3,
    }
}
