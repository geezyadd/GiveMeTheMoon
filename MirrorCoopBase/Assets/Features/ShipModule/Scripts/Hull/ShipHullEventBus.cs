using System;
using Features.GameCoreModule.Contracts;

namespace Features.ShipModule.Scripts.Hull {
    public sealed class ShipHullEventBus : IShipHullEvents, IShipHullEventPublisher {
        public event Action<ShipDamage, float> OnDamaged;
        public event Action OnDestroyed;

        public void PublishDamaged(ShipDamage damage, float hullAfter) =>
            OnDamaged?.Invoke(damage, hullAfter);

        public void PublishDestroyed() =>
            OnDestroyed?.Invoke();
    }
}
