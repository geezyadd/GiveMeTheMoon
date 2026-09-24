using System;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories {
    public class StatEntityFactoryBase<TStatEnum> : IStatEntityFactory<TStatEnum> where TStatEnum : Enum  {
        private readonly IStatFactory<TStatEnum> _statFactory;

        public StatEntityFactoryBase(IStatFactory<TStatEnum> statFactory) =>
            _statFactory = statFactory;

        public StatEntityBase<TStatEnum> Create() =>
            new(_statFactory);
    }
}