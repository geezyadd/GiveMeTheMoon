using System;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using UnityEngine;
using Zenject;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity {
    public class StatEntityMonoBase<TStatEnum> : MonoBehaviour, IModifierEntity<TStatEnum>, IStatEntity<TStatEnum> where TStatEnum : Enum {
        private IStatEntityFactory<TStatEnum> _statEntityFactory;
        private StatEntityBase<TStatEnum> StatEntityBase {
            get =>
                _statEntityBase ??= _statEntityFactory.Create();
            set =>
                _statEntityBase = value;
        }

        private StatEntityBase<TStatEnum> _statEntityBase;

        [Inject]
        private void InjectDependencies(IStatEntityFactory<TStatEnum> statEntityFactory) =>
            _statEntityFactory = statEntityFactory;

        public void AddModifier(TStatEnum statType, StatModifier statModifier) =>
            StatEntityBase.AddModifier(statType, statModifier);

        public void RemoveModifier(TStatEnum statType, StatModifier statModifier) =>
            StatEntityBase.RemoveModifier(statType, statModifier);

        public void RemoveModifierThatEqual(TStatEnum statType, StatModifier statModifier) =>
            StatEntityBase.RemoveModifierThatEqual(statType, statModifier);

        public void RemoveAllModifiers() =>
            StatEntityBase.RemoveAllModifiers();

        public IStat this[TStatEnum entityStatType] =>
            StatEntityBase[entityStatType];
        
        public IStat GetStat(TStatEnum entityStatType) =>
            StatEntityBase.GetStat(entityStatType);

        public void ClearStats() {
            StatEntityBase.ClearStats();
        }

        public event Action<TStatEnum> OnReachedMinValue {
            add =>
                StatEntityBase.OnReachedMinValue += value;
            remove =>
                StatEntityBase.OnReachedMinValue -= value;
        }
        public event Action<TStatEnum> OnReachedMaxValue {
            add =>
                StatEntityBase.OnReachedMaxValue += value;
            remove =>
                StatEntityBase.OnReachedMaxValue -= value;
        }
        public event Action<TStatEnum> OnBaseValueChanged {
            add =>
                StatEntityBase.OnBaseValueChanged += value;
            remove =>
                StatEntityBase.OnBaseValueChanged -= value;
        }
        public event Action<TStatEnum> OnBonusValueChanged {
            add =>
                StatEntityBase.OnBonusValueChanged += value;
            remove =>
                StatEntityBase.OnBonusValueChanged -= value;
        }

        public event Action<TStatEnum> OnFullValueChanged {
            add =>
                StatEntityBase.OnFullValueChanged += value;
            remove =>
                StatEntityBase.OnFullValueChanged -= value;
        }

        public event Action<TStatEnum> OnMinValueChanged {
            add =>
                StatEntityBase.OnMinValueChanged += value;
            remove =>
                StatEntityBase.OnMinValueChanged -= value;
        }
        public event Action<TStatEnum> OnMaxValueChanged {
            add =>
                StatEntityBase.OnMaxValueChanged += value;
            remove =>
                StatEntityBase.OnMaxValueChanged -= value;
        }
    }
}