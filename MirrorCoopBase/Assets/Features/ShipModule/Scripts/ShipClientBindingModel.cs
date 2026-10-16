namespace Features.ShipModule.Scripts {
    // The run's ship as this client sees it; bound on every client, the host included, while the director's client
    // is started.
    public sealed class ShipClientBindingModel : IShipClientBindingModel {
        public ShipBase Ship { get; private set; }

        public void Bind(ShipBase ship) =>
            Ship = ship;

        public void Clear() =>
            Ship = null;
    }
}
