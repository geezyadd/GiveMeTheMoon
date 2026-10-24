namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipServerImpactEventPublisher {
        public void PublishServerImpact(ShipRockImpact impact);
    }
}
