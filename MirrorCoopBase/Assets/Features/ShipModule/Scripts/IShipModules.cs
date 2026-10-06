namespace Features.ShipModule.Scripts {
    internal interface IShipModules {
        public void ServerOnModuleInstalled(ShipSocket socket);
        public void ServerOnModuleUninstalled(ShipSocket socket);
        public float GetStatFull(ShipStatType type);
        public bool HasControlModule();
    }
}
