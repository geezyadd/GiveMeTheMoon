using Mirror;

namespace Features.ShipModule.Scripts.Overboard {
    public interface IShipOverboardEventPublisher {
        public void PublishDriftLimitReached(NetworkIdentity player);
    }
}
