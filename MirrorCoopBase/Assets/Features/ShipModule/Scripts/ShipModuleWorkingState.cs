using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipModule.Scripts {
    // Server: the bound ship's module stats behind the contract the damage module uses to break and repair a module.
    public sealed class ShipModuleWorkingState : IShipModuleWorkingState {
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipModuleWorkingState(IShipRunBindingModel shipRunBindingModel) =>
            _shipRunBindingModel = shipRunBindingModel;

        public void ServerSetModuleWorking(string socketId, bool isWorking) =>
            _shipRunBindingModel.Ship.Modules.ServerSetModuleWorking(socketId, isWorking);
    }
}
