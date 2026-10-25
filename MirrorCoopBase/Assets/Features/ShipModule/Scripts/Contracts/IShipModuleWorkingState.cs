namespace Features.ShipModule.Scripts.Contracts {
    // Server: suspends or resumes the stat modifiers of the module in one socket of the bound ship.
    public interface IShipModuleWorkingState {
        public void ServerSetModuleWorking(string socketId, bool isWorking);
    }
}
