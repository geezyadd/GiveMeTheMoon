using System;

namespace Features.ShipModule.Scripts.Impacts {
    // Server only: fires once per rock hit it decided, before the hit goes out to the peers.
    public interface IShipServerImpactEvents {
        public event Action<ShipRockImpact> OnServerImpact;
    }
}
