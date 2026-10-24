namespace Features.ShipModule.Scripts {
    internal interface IShipModules {
        public void ServerOnModuleInstalled(ShipSocket socket, ModuleDefinition module);
        public void ServerOnModuleUninstalled(ShipSocket socket);
        public void ServerPublishStats();
        public void ServerSetModuleWorking(string socketId, bool isWorking);
        public float GetStatFull(ShipStatType type);
        public bool HasControlModule();
    }
}
