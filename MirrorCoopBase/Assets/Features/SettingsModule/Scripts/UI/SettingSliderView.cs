using System;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SettingsModule.UI {
    public abstract class SettingSliderViewBase : SettingElementViewBase {
        public Action<float> OnValueChanged;

        public abstract SliderSettingDefinition SliderDefinition { get; }

        public override SettingDefinition Definition => SliderDefinition;

        public abstract void SetLabel(string label);
        public abstract void SetRange(float minimum, float maximum);
        public abstract void SetValue(float value, string valueText);
    }

    // A number row: label, slider and the value as text.
    public sealed class SettingSliderView : SettingSliderViewBase {
        [SerializeField] private SliderSettingDefinition _definition;
        [SerializeField] private Text _label;
        [SerializeField] private Slider _slider;
        [SerializeField] private Text _valueText;

        public override SliderSettingDefinition SliderDefinition => _definition;

        protected override void OnEnable() {
            base.OnEnable();
            _slider.onValueChanged.AddListener(OnSliderChanged);
        }

        protected override void OnDisable() {
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
            base.OnDisable();
        }

        public override void SetLabel(string label) =>
            _label.text = label;

        public override void SetRange(float minimum, float maximum) {
            _slider.wholeNumbers = false;
            _slider.minValue = minimum;
            _slider.maxValue = maximum;
        }

        public override void SetValue(float value, string valueText) {
            _slider.SetValueWithoutNotify(value);
            _valueText.text = valueText;
        }

        private void OnSliderChanged(float value) =>
            OnValueChanged?.Invoke(value);
    }
}
