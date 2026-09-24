using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;

namespace Features.ShipModule.Scripts {
    public sealed class ShipStatFactory : StatFactoryBase<ShipStatType> {
        public ShipStatFactory(ShipAccumulativeStatsConfiguration configuration) : base(configuration) {
        }
    }
}
