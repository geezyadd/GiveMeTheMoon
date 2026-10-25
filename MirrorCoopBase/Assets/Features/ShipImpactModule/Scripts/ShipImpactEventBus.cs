using System;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public sealed class ShipImpactEventBus : IShipImpactEvents, IShipImpactEventPublisher {
        public event Action<ShipRockImpact> OnImpact;

        public void PublishImpact(ShipRockImpact impact) =>
            OnImpact?.Invoke(impact);
    }
}
