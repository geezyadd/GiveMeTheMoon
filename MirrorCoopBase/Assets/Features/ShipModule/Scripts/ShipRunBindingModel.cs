namespace Features.ShipModule.Scripts {
    public sealed class ShipRunBindingModel : IShipRunBindingModel {
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
