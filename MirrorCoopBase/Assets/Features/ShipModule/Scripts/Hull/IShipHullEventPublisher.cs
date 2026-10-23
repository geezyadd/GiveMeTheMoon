using Features.GameCoreModule.Contracts;

namespace Features.ShipModule.Scripts.Hull {
    public interface IShipHullEventPublisher {
        public void PublishDamaged(ShipDamage damage, float hullAfter);
        public void PublishDestroyed();
    }
}
