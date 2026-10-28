using System;
using Mirror;

namespace Features.ShipModule.Scripts.Overboard {
    public sealed class ShipOverboardEventBus : IShipOverboardEvents, IShipOverboardEventPublisher {
        public event Action<NetworkIdentity> OnDriftLimitReached;

        public void PublishDriftLimitReached(NetworkIdentity player) =>
            OnDriftLimitReached?.Invoke(player);
    }
}
