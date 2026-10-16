namespace Features.ShipModule.Scripts {
    public interface IShipClientBindingModel {
        public ShipBase Ship { get; }
        public void Bind(ShipBase ship);
        public void Clear();
    }
}
