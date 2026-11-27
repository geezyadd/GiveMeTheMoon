using UnityEngine;

namespace Features.SettingsModule.Data {
    // A number between a minimum and a maximum, moved in steps. The value is kept in the units the player sees
    // (e.g. 50 for 50 %); the module that applies it converts.
    [CreateAssetMenu(
        fileName = nameof(SliderSettingDefinition),
        menuName = "Configurations/SettingsModule/" + nameof(SliderSettingDefinition))]
    public sealed class SliderSettingDefinition : SettingDefinition {
        [SerializeField] private float _minimum;
        [SerializeField] private float _maximum = 1f;
        [Tooltip("The slider moves in steps of this size from the minimum; 0 means no steps.")]
        [SerializeField] private float _step;
        [SerializeField] private float _defaultValue;
        [Tooltip("string.Format pattern of the value text next to the slider, e.g. {0:0}%.")]
        [SerializeField] private string _valueFormat = "{0:0.##}";

        public float Minimum => _minimum;
        public float Maximum => _maximum;
        public float Step => _step;
        public float DefaultValue => _defaultValue;
        public string ValueFormat => _valueFormat;

        // The nearest value the slider can take: clamped to the range and snapped to a step.
        public float Normalize(float value) {
            float clamped = Mathf.Clamp(value, _minimum, _maximum);
            if (_step <= 0f)
                return clamped;

            float snapped = _minimum + Mathf.Round((clamped - _minimum) / _step) * _step;
            return Mathf.Clamp(snapped, _minimum, _maximum);
        }

        public override void WriteDefault(SettingsModel model) =>
            model.SetSlider(Key, _defaultValue);
    }
}
