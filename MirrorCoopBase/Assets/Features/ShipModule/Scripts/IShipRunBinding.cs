namespace Features.ShipModule.Scripts {
    public interface IShipRunBinding {
        ShipRunDirector Director { get; }
        ShipBase Ship { get; }
        void Bind(ShipRunDirector director, ShipBase ship);
        void Clear();
    }
}
