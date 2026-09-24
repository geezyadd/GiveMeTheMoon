using System;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity {
    public interface IStatEntity<TStatEnum> where TStatEnum : Enum {
        public IStat this[TStatEnum entityStatType] { get; }

        public IStat GetStat(TStatEnum statType);

        public void ClearStats();

        public event Action<TStatEnum> OnReachedMinValue;
        public event Action<TStatEnum> OnReachedMaxValue;
        public event Action<TStatEnum> OnBaseValueChanged;
        public event Action<TStatEnum> OnBonusValueChanged;
        public event Action<TStatEnum> OnFullValueChanged;
        public event Action<TStatEnum> OnMinValueChanged;
        public event Action<TStatEnum> OnMaxValueChanged;
    }
}