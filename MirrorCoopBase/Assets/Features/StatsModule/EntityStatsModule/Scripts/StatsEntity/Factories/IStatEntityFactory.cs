using System;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories {
    public interface IStatEntityFactory<TStatEnum> where TStatEnum : Enum{
        StatEntityBase<TStatEnum> Create();
    }
}