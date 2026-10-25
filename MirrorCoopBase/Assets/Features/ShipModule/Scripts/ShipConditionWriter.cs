using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipModule.Scripts {
    // Server: the bound ship's condition bridge behind the contract, so the damage module never touches the bridge.
    public sealed class ShipConditionWriter : IShipConditionWriter {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipConditionWriter(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public bool ServerAddBrokenSockets(string socketId) =>
            _shipRunBindingModel.Ship.ConditionSync.ServerAddBrokenSockets(socketId);

        public bool ServerRemoveBrokenSockets(string socketId) =>
            _shipRunBindingModel.Ship.ConditionSync.ServerRemoveBrokenSockets(socketId);

        public bool ServerAddOpenBreaches(string pointId) =>
            _shipRunBindingModel.Ship.ConditionSync.ServerAddOpenBreaches(pointId);

        public bool ServerRemoveOpenBreaches(string pointId) =>
            _shipRunBindingModel.Ship.ConditionSync.ServerRemoveOpenBreaches(pointId);

        public void ServerClearOpenBreaches() =>
            _shipRunBindingModel.Ship.ConditionSync.ServerClearOpenBreaches();
    }
}
