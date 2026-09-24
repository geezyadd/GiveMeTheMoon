using System;
using System.Collections.Generic;
using System.Linq;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using UnityEngine;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Stats {
    /// <summary>
    /// Whenever modifiers are changed, bonus value is recalculated.
    /// Bonus value is not meant to be directly modified.
    /// Addition and subtraction directly changes base value.
    ///
    /// Example usage - Enemy attack speed.
    /// </summary>
    public sealed class StaticValueStat : IStat {
        private const float PERCENTAGE_SCALE = 1;
        
        private float _minValue;
        private float _maxValue;
        private float _bonusValue;
        private float _value;
        
        private bool _hasPreviouslyReachedMinValue;
        private bool _hasPreviouslyReachedMaxValue;
        
        public List<StatModifier> StatModifiers { get; } = new();
        
        public float FullValue =>
            _value + _bonusValue;
        
        public float NonModifiedMaxValue =>
            _maxValue;
        
        public float MaxValue {
            get =>
                _maxValue;
            set {
                _maxValue = value;
                OnMaxValueChanged?.Invoke(_maxValue);
            }
        }

        public float MinValue {
            get =>
                _minValue;
            set {
                _minValue = value;
                OnMinValueChanged?.Invoke(_minValue);
            }
        }

        public float BonusValue {
            get =>
                _bonusValue;
            private set {
                _bonusValue = value;
                OnBonusValueChanged?.Invoke(_bonusValue);
                OnFullValueChanged?.Invoke(FullValue);
            }
        }

        public float Value {
            get =>
                _value;
            private set {
                _value = value;
                OnValueChanged?.Invoke(_value);
                OnFullValueChanged?.Invoke(FullValue);
                
                bool isMaxValueReached = Mathf.Approximately(Value, MaxValue);
                switch (isMaxValueReached) {
                    case true when _hasPreviouslyReachedMaxValue is false:
                        OnReachedMaxValue?.Invoke();
                        _hasPreviouslyReachedMaxValue = true;
                        break;
                    case false:
                        _hasPreviouslyReachedMaxValue = false;
                        break;
                }

                bool isMinValueReached = Mathf.Approximately(Value, MinValue);
                switch (isMinValueReached) {
                    case true when _hasPreviouslyReachedMinValue is false:
                        OnReachedMinValue?.Invoke();
                        _hasPreviouslyReachedMinValue = true;
                        break;
                    case false:
                        _hasPreviouslyReachedMinValue = false;
                        break;
                }
            }
        }

        public event Action OnReachedMinValue;
        public event Action OnReachedMaxValue;
        public event Action<float> OnValueChanged;
        public event Action<float> OnBonusValueChanged;
        public event Action<float> OnFullValueChanged;
        public event Action<float> OnMinValueChanged;
        public event Action<float> OnMaxValueChanged;
        
        public void AddValue(float value) {
            float newValue = value + Value;
            Value = Mathf.Min(newValue, MaxValue);
        }

        public void Subtract(float subtractionValue) {
            float newValue = Value - subtractionValue;
            Value = Mathf.Max(newValue, MinValue);
        }

        public void OverrideValue(float value) =>
            Value = value;

        public void AddStatModifier(StatModifier statModifier) {
            StatModifiers.Add(statModifier);
            StatModifiers.Sort((firstValue, secondValue) => firstValue.Order.CompareTo(secondValue.Order));

            BonusValue = CalculateBonusValue(StatModifiers);
        }

        public void RemoveStatModifier(StatModifier statModifier) {
            if (StatModifiers == null || !StatModifiers.Contains(statModifier))
                return;

            StatModifiers.Remove(statModifier);

            BonusValue = CalculateBonusValue(StatModifiers);
        }

        public void RemoveStatModifierThatEqual(StatModifier statModifier) {
            StatModifier firstOrDefault = StatModifiers?.FirstOrDefault(s => s == statModifier);
            if (firstOrDefault == null)
                return;

            StatModifiers.Remove(statModifier);

            BonusValue = CalculateBonusValue(StatModifiers);
        }

        public void ClearModifiers() {
            StatModifiers.Clear();
            BonusValue = CalculateBonusValue(StatModifiers);
        }

        public float GetPreviewModifier(ICollection<StatModifier> newStatModifiers) {
            List<StatModifier> allStatModifiers = new(StatModifiers);
            allStatModifiers.AddRange(newStatModifiers);
            allStatModifiers.Sort((firstValue, secondValue) => firstValue.Order.CompareTo(secondValue.Order));
            float previewBonusValue = CalculateBonusValue(allStatModifiers);
            return previewBonusValue + Value;
        }

        public void Clear() {
            OnReachedMinValue = null;
            OnReachedMaxValue = null;
            OnValueChanged = null;
            OnBonusValueChanged = null;
            OnFullValueChanged = null;
            OnMinValueChanged = null;
            OnMaxValueChanged = null;
        }
        
        private float CalculateBonusValue(ICollection<StatModifier> modifiers) =>
            modifiers.Aggregate(Value, (current, statModifier) => ApplyModifier(statModifier, current)) - Value;

        private float ApplyModifier(StatModifier statModifier, float finalValue) {
            switch (statModifier.ModifierType) {
                case ModifierType.Flat:
                    finalValue += statModifier.Value;
                    break;
                case ModifierType.PercentAdd:
                    finalValue *= PERCENTAGE_SCALE + statModifier.Value;
                    break;
                case ModifierType.PercentMulti:
                    finalValue *= statModifier.Value;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(statModifier.ModifierType), "Unknown modifier type");
            }

            return finalValue;
        }
    }
}