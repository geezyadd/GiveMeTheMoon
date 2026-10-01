using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;

namespace Features.CharacterMovableModule.Scripts.PlayerStats {
    public sealed class PlayerStatEntityFactory : StatEntityFactoryBase<PlayerStatType> {
        public PlayerStatEntityFactory(IStatFactory<PlayerStatType> statFactory) : base(statFactory) { }
    }
}
