namespace Features.ShipModule.Scripts.Overboard {
    // What the server does with one loose item this check.
    public enum ShipOverboardItemDecision {
        None = 0,
        // It left every safe space: gravity off, its speed fades to a slow drift.
        BeginDrift = 1,
        // It is right over a floor again: gravity back, it falls onto the floor.
        EndDrift = 2,
        // It drifted too far from the ship and every area: it is removed.
        Remove = 3,
    }
}
