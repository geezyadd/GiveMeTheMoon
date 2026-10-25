using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipServerImpactEventPublisher {
        public void PublishServerImpact(ShipRockImpact impact);
    }
}
