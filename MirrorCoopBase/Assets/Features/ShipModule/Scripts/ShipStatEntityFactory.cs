using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;

namespace Features.ShipModule.Scripts {
    public sealed class ShipStatEntityFactory : StatEntityFactoryBase<ShipStatType> {
        public ShipStatEntityFactory(IStatFactory<ShipStatType> statFactory) : base(statFactory) {
        }
    }
}
