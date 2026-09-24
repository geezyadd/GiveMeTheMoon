using System;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories {
    public interface IStatFactory<in TStatEnum>  where TStatEnum : Enum{
        IStat Create(TStatEnum entityStatType);
    }
}