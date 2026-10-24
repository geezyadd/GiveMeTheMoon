namespace Features.ShipModule.Scripts.Impacts {
    public enum ShipRockImpactKind {
        None = 0,
        // A small breakable rock: it shatters and damages the hull.
        Small = 1,
        // Everything else: the rock stays and knocks the ship back.
        Large = 2,
    }
}
