namespace Features.ShipModule.Scripts {
    internal interface IShipModules {
        public void ServerOnModuleInstalled(ShipSocket socket, ModuleDefinition module);
        public void ServerOnModuleUninstalled(ShipSocket socket);
        public void ServerPublishStats();
        public float GetStatFull(ShipStatType type);
        public bool HasControlModule();
    }
}
