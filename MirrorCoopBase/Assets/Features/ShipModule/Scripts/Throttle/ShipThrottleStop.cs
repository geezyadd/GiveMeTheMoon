namespace Features.ShipModule.Scripts.Throttle {
    public sealed class ShipThrottleStop : IShipThrottleStop {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipThrottleStop(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public void ServerStop() =>
            _shipRunBindingModel.Ship.Throttle.ServerReset();
    }
}
