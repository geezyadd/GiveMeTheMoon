using System;
using System.Collections.Generic;
using System.Linq;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using UnityEngine;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Stats {
    /// <summary>
    /// Modifiers influence MAX value.
    /// Whenever MAX value is changed, base value is adjusted.
    /// Bonus value is not influenced by modifiers.
    /// Subtraction attempts to subtract bonus values before attempting to subtract base value.
    ///
    /// Example usage - player HP.
    /// </summary>
    public sealed class AccumulativeStat : IStat {
        private const float PERCENTAGE_SCALE = 1;

        private float _minValue;
        private float _defaultBaseValue;
        private float _modifiedMaxValue;
        private float _bonusValue;
        private float _value;

        private bool _hasPreviouslyReachedMinValue;
        private bool _hasPreviouslyReachedMaxValue;

        public List<StatModifier> StatModifiers { get; } = new();

        public float FullValue { get; private set; }
        
        public float NonModifiedMaxValue =>
            _defaultBaseValue;

        public float MaxValue {
            get => 
                _modifiedMaxValue;
            set {
                _defaultBaseValue = value;
                _modifiedMaxValue = CalculateValueWithModifiers(_defaultBaseValue, StatModifiers);
                OnMaxValueChanged?.Invoke(_modifiedMaxValue);
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
            get => _bonusValue;
            private set {
                _bonusValue = value;
                OnBonusValueChanged?.Invoke(_bonusValue);
                FullValue = _value + _bonusValue;
                OnFullValueChanged?.Invoke(FullValue);
            }
        }

        public float Value {
            get =>
                _value;
            private set {
                _value = value;
                OnValueChanged?.Invoke(_value);
                FullValue = _value + _bonusValue;
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
            if (BonusValue > 0) {
                float newSubtractValue = subtractionValue - BonusValue;
                if (newSubtractValue > 0)
                    SubtractValue(newSubtractValue);
                else
                    BonusValue -= subtractionValue;
            }
            else
                SubtractValue(subtractionValue);
        }

        public void OverrideValue(float value) =>
            Value = Mathf.Min(value, MaxValue);

        public void AddStatModifier(StatModifier statModifier) {
            float previousMaxValue = MaxValue;
            
            StatModifiers.Add(statModifier);
            StatModifiers.Sort((firstValue, secondValue) => firstValue.Order.CompareTo(secondValue.Order));
            
            MaxValue = _defaultBaseValue;
            CalculateValueRelativeToMaxValueDelta(MaxValue - previousMaxValue);
        }

        public void RemoveStatModifier(StatModifier statModifier) {
            if (StatModifiers == null || !StatModifiers.Contains(statModifier))
                return;

            float previousMaxValue = MaxValue;
            StatModifiers.Remove(statModifier);
            
            MaxValue = _defaultBaseValue;
            CalculateValueRelativeToMaxValueDelta(MaxValue - previousMaxValue);
        }

        public void RemoveStatModifierThatEqual(StatModifier statModifier) {
            StatModifier firstOrDefault = StatModifiers?.FirstOrDefault(s => s == statModifier);
            if (firstOrDefault == null)
                return;

            float previousMaxValue = MaxValue;
            StatModifiers.Remove(firstOrDefault);
            
            MaxValue = _defaultBaseValue;
            CalculateValueRelativeToMaxValueDelta(MaxValue - previousMaxValue);
        }

        public void ClearModifiers() {
            float previousMaxValue = MaxValue;
            
            StatModifiers.Clear();
            
            MaxValue = _defaultBaseValue;
            CalculateValueRelativeToMaxValueDelta(MaxValue - previousMaxValue);
        }

        public float GetPreviewModifier(ICollection<StatModifier> newStatModifiers) {
            float previewValue = CalculateValueWithModifiers(MaxValue, newStatModifiers);
            return previewValue;
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

        private void CalculateValueRelativeToMaxValueDelta(float maxValueDelta) {
            switch (maxValueDelta) {
                case < 0:
                    Value = Mathf.Min(Value, MaxValue);
                    break;
                case > 0:
                    AddValue(maxValueDelta);
                    break;
            }
        }

        private float CalculateValueWithModifiers(float initialValue, ICollection<StatModifier> modifiers) =>
            modifiers.Aggregate(initialValue, (current, statModifier) => ApplyModifier(statModifier, current));

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

        private void SubtractValue(float subtractionValue) {
            float newValue;
            if (Value <= subtractionValue)
                newValue = MinValue;
            else
                newValue = Value - subtractionValue;
            
            Value = newValue;
        }
    }
}