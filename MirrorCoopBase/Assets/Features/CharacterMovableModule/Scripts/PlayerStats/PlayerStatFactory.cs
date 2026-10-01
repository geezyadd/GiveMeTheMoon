using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;

namespace Features.CharacterMovableModule.Scripts.PlayerStats {
    public sealed class PlayerStatFactory : StatFactoryBase<PlayerStatType> {
        public PlayerStatFactory(PlayerStatsConfiguration configuration) : base(configuration) { }
    }
}
