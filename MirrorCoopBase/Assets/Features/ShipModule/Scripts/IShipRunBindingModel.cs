namespace Features.ShipModule.Scripts {
    public interface IShipRunBindingModel {
        public ShipRunDirector Director { get; }
        public ShipBase Ship { get; }
        public void Bind(ShipRunDirector director, ShipBase ship);
        public void Clear();
    }
}
