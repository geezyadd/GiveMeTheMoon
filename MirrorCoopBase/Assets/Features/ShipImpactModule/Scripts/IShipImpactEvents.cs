using System;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    // Every peer (the host too): fires once per rock hit the server decided.
    public interface IShipImpactEvents {
        public event Action<ShipRockImpact> OnImpact;
    }
}
