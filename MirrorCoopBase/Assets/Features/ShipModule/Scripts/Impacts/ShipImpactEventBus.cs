using System;

namespace Features.ShipModule.Scripts.Impacts {
    public sealed class ShipImpactEventBus : IShipImpactEvents, IShipImpactEventPublisher {
        public event Action<ShipRockImpact> OnImpact;

        public void PublishImpact(ShipRockImpact impact) =>
            OnImpact?.Invoke(impact);
    }
}
