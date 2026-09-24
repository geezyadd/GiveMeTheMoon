using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using UnityEngine;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity {
    public class StatEntityBase<TStatEnum> : IModifierEntity<TStatEnum>, IStatEntity<TStatEnum> where TStatEnum : Enum {
        private readonly IStatFactory<TStatEnum> _statFactory;

        public StatEntityBase(IStatFactory<TStatEnum> statFactory) {
            _statFactory = statFactory;
        }
        
        private readonly Dictionary<TStatEnum, IStat> _statsHolder = new(new EntityStatComparator());

        public IStat this[TStatEnum TStatEnum] {
            get {
                if (_statsHolder.ContainsKey(TStatEnum) == false)
                    AddStat(TStatEnum);
            
                return _statsHolder[TStatEnum];
            }
        }

        public event Action<TStatEnum> OnReachedMinValue;

        public event Action<TStatEnum> OnReachedMaxValue;

        public event Action<TStatEnum> OnBaseValueChanged;

        public event Action<TStatEnum> OnBonusValueChanged;

        public event Action<TStatEnum> OnFullValueChanged;

        public event Action<TStatEnum> OnMinValueChanged;

        public event Action<TStatEnum> OnMaxValueChanged;
        
        public IStat GetStat(TStatEnum statType) {
            if (_statsHolder.ContainsKey(statType) == false)
                AddStat(statType);
            
            return _statsHolder[statType];
        }

        public void AddModifier(TStatEnum statType, StatModifier statModifier) {
            if (!_statsHolder.TryGetValue(statType, out IStat stat))
                return;

            stat.AddStatModifier(statModifier);
        }

        public void RemoveModifier(TStatEnum statType, StatModifier statModifier) {
            if (!_statsHolder.TryGetValue(statType, out IStat stat)) {
                return;
            }

            stat.RemoveStatModifier(statModifier);
        }

        public void RemoveModifierThatEqual(TStatEnum statType, StatModifier statModifier) {
            if (!_statsHolder.TryGetValue(statType, out IStat stat)) {
                return;
            }

            stat.RemoveStatModifierThatEqual(statModifier);
        }

        public void RemoveAllModifiers() {
            foreach (IStat stat in _statsHolder.Values)
                stat.ClearModifiers();
        }

        private void AddStat(TStatEnum statType) {
            if (_statsHolder.ContainsKey(statType)) {
                Debug.LogError($"Stats Holder already contains {statType}!");
                return;
            }

            IStat stat = _statFactory.Create(statType);
            _statsHolder.Add(statType, stat);
            stat.OnReachedMinValue += () => OnReachedMinValue?.Invoke(statType);
            stat.OnReachedMaxValue += () => OnReachedMaxValue?.Invoke(statType);
            stat.OnValueChanged += _ => OnBaseValueChanged?.Invoke(statType);
            stat.OnBonusValueChanged += _ => OnBonusValueChanged?.Invoke(statType);
            stat.OnFullValueChanged += _ => OnFullValueChanged?.Invoke(statType);
            stat.OnMinValueChanged += _ => OnMinValueChanged?.Invoke(statType);
            stat.OnMaxValueChanged += _ => OnMaxValueChanged?.Invoke(statType);
        }

        public void ClearStats() {
            foreach (IStat stat in _statsHolder.Values)
                stat.Clear();
        }

        
        // Exists to avoid boxing
        private class EntityStatComparator : IEqualityComparer<TStatEnum> {
            public bool Equals(TStatEnum x, TStatEnum y) =>
                x.Equals(y);

            public int GetHashCode(TStatEnum x) =>
                Convert.ToInt32(x);
        }
    }
}