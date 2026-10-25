using System;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public sealed class ShipServerImpactEventBus : IShipServerImpactEvents, IShipServerImpactEventPublisher {
        public event Action<ShipRockImpact> OnServerImpact;

        public void PublishServerImpact(ShipRockImpact impact) =>
            OnServerImpact?.Invoke(impact);
    }
}
