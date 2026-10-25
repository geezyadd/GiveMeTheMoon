namespace Features.ShipModule.Scripts {
    // Whether the ship can take off now, and why not.
    public enum ShipLaunchReadiness {
        None = 0,
        Ready = 1,
        NotAtStation = 2,
        RouteEnded = 3,
        MissingRequiredModule = 4
    }
}
