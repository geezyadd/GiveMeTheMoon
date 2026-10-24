using System;

namespace Features.ShipModule.Scripts.Impacts {
    public sealed class ShipServerImpactEventBus : IShipServerImpactEvents, IShipServerImpactEventPublisher {
        public event Action<ShipRockImpact> OnServerImpact;

        public void PublishServerImpact(ShipRockImpact impact) =>
            OnServerImpact?.Invoke(impact);
    }
}
