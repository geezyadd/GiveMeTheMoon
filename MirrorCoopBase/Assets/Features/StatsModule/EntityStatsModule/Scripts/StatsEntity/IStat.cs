using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;

namespace Features.StatsModule.EntityStatsModule.Scripts.StatsEntity {
    public interface IStat {
        public List<StatModifier> StatModifiers { get; }
        public float NonModifiedMaxValue { get; }
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
        
        // Usually Value+BonusValue
        public float FullValue { get; }
        // Separate value that is usually applied on top of Value
        public float BonusValue { get; }
        // Base value
        public float Value { get; }

        public event Action OnReachedMinValue;
        public event Action OnReachedMaxValue;
        public event Action<float> OnValueChanged;
        public event Action<float> OnBonusValueChanged;
        public event Action<float> OnFullValueChanged;
        public event Action<float> OnMinValueChanged;
        public event Action<float> OnMaxValueChanged;

        public void AddValue(float value);
        public void Subtract(float subtractionValue);
        public void OverrideValue(float value);

        public void AddStatModifier(StatModifier statModifier);
        public void RemoveStatModifier(StatModifier statModifier);
        public void RemoveStatModifierThatEqual(StatModifier statModifier);
        public void ClearModifiers();
        public float GetPreviewModifier(ICollection<StatModifier> newStatModifiers);
        public void Clear();
    }
}