using System;

namespace Features.ShipModule.Scripts.Impacts {
    // Every peer (the host too): fires once per rock hit the server decided.
    public interface IShipImpactEvents {
        public event Action<ShipRockImpact> OnImpact;
    }
}
