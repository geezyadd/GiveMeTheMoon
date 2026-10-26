using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipImpactEventPublisher {
        public void PublishImpact(ShipRockImpact impact);
    }
}
