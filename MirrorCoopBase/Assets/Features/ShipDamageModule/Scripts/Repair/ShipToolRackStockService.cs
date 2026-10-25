using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipDamageModule.Scripts.Repair {
    // Server: at the start of every run each enabled tool rack of the ship puts its starting tools out.
    public sealed class ShipToolRackStockService : IShipRunStartListener {
        private readonly IReadOnlyComponentRegistry<ShipToolRack> _shipToolRackRegistry;

        public ShipToolRackStockService(IReadOnlyComponentRegistry<ShipToolRack> shipToolRackRegistry) =>
            _shipToolRackRegistry = shipToolRackRegistry;

        public void ServerOnRunStarted() {
            foreach (ShipToolRack rack in _shipToolRackRegistry.Items)
                rack.ServerStock();
        }
    }
}
