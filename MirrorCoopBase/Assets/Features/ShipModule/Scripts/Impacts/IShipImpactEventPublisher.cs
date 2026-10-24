namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipImpactEventPublisher {
        public void PublishImpact(ShipRockImpact impact);
    }
}
