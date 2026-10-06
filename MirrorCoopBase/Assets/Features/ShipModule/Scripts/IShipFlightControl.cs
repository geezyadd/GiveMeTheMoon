namespace Features.ShipModule.Scripts {
    internal interface IShipFlightControl {
        public ShipFlightMode Mode { get; }
        public bool IsTakeoffComplete { get; }
        public bool HasLanded { get; }
        public void LockControls();
    }
}
