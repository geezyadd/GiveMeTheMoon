namespace Features.GameCoreModule.Contracts {
    public enum ShipDamageCause {
        None = 0,
        Debug = 1,
        Collision = 2,
        // An open hull breach slowly losing HP.
        HullLeak = 3
    }
}
