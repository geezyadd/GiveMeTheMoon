namespace Features.ShipModule.Scripts {
    // The director and ship of the run in progress; both are null outside a run (menu, lobby).
    internal sealed class ShipRunBinding : IShipRunBinding {
        public ShipRunDirector Director { get; private set; }
        public ShipBase Ship { get; private set; }

        public void Bind(ShipRunDirector director, ShipBase ship) {
            Director = director;
            Ship = ship;
        }

        public void Clear() {
            Director = null;
            Ship = null;
        }
    }
}
