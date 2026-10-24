using Features.ShipModule.Scripts.Hull;

namespace Features.ShipModule.Scripts.Damage {
    // The station's free repair on landing: the hull, the broken modules and the breaches, all under the hull's
    // RepairOnLanding flag.
    public sealed class ShipLandingRepair : IShipLandingRepair {
        private readonly IShipHullRepair _shipHullRepair;
        private readonly IShipConditionService _shipConditionService;
        private readonly ShipHullConfiguration _shipHullConfiguration;

        public ShipLandingRepair(IShipHullRepair shipHullRepair, IShipConditionService shipConditionService, ShipHullConfiguration shipHullConfiguration) {
            _shipHullRepair = shipHullRepair;
            _shipConditionService = shipConditionService;
            _shipHullConfiguration = shipHullConfiguration;
        }

        public void ServerRepairAfterLanding() {
            _shipHullRepair.ServerRepairAfterLanding();
            if (_shipHullConfiguration.RepairOnLanding)
                _shipConditionService.ServerRepairAll();
        }
    }
}
