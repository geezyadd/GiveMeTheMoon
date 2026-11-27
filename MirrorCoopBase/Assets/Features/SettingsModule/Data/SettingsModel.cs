using System;
using System.Collections.Generic;

namespace Features.SettingsModule.Data {
    // The current value of every player setting of this machine (local, not networked). It starts from the
    // definitions' defaults. Modules that apply a setting read its value and listen to OnValueChanged.
    //
    // Save system seam: loaded values are written with SetToggle / SetSlider / SetChoice (check AcceptsOption of a
    // choice first), and the values to save are read the same way for each of Definitions. A module that applies a
    // setting reacts to the change event, so writing loaded values after startup is enough.
    public sealed class SettingsModel {
        private readonly Dictionary<string, SettingDefinition> _definitionByKey = new();
        private readonly List<SettingDefinition> _definitions = new();
        private readonly Dictionary<string, bool> _toggleValues = new();
        private readonly Dictionary<string, float> _sliderValues = new();
        private readonly Dictionary<string, string> _choiceValues = new();

        // The key of the setting whose value has just changed.
        public event Action<string> OnValueChanged;

        public IReadOnlyList<SettingDefinition> Definitions => _definitions;

        public SettingsModel(SettingsCatalog catalog) {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            foreach (SettingDefinition definition in catalog.Settings)
                AddDefinition(definition);

            foreach (SettingDefinition definition in _definitions)
                definition.WriteDefault(this);
        }

        public bool Contains(string key) =>
            key != null && _definitionByKey.ContainsKey(key);

        public SettingDefinition GetDefinition(string key) =>
            _definitionByKey.TryGetValue(key, out SettingDefinition definition)
                ? definition
                : throw new KeyNotFoundException($"No setting with key '{key}'.");

        public bool GetToggle(string key) {
            GetDefinition<ToggleSettingDefinition>(key);
            return _toggleValues[key];
        }

        public float GetSlider(string key) {
            GetDefinition<SliderSettingDefinition>(key);
            return _sliderValues[key];
        }

        // The slider's value as a multiple of its default (see SliderSettingDefinition.ScaleToDefault).
        public float GetSliderScaleToDefault(string key) =>
            GetDefinition<SliderSettingDefinition>(key).ScaleToDefault(_sliderValues[key]);

        public string GetChoice(string key) {
            GetDefinition<ChoiceSettingDefinitionBase>(key);
            return _choiceValues[key];
        }

        public void SetToggle(string key, bool value) {
            GetDefinition<ToggleSettingDefinition>(key);
            SetValue(_toggleValues, key, value);
        }

        // The value is clamped to the slider's range and snapped to its step.
        public void SetSlider(string key, float value) {
            SliderSettingDefinition definition = GetDefinition<SliderSettingDefinition>(key);
            SetValue(_sliderValues, key, definition.Normalize(value));
        }

        // Throws for an option the setting does not have.
        public void SetChoice(string key, string optionId) {
            ChoiceSettingDefinitionBase definition = GetDefinition<ChoiceSettingDefinitionBase>(key);
            if (definition.AcceptsOption(optionId) == false)
                throw new ArgumentException($"Setting '{key}' has no option '{optionId}'.", nameof(optionId));

            SetValue(_choiceValues, key, optionId);
        }

        public void ResetToDefault(string key) =>
            GetDefinition(key).WriteDefault(this);

        // Takes the other model's value of every setting both have (a settings window's draft and the live model).
        public void CopyValuesFrom(SettingsModel other) {
            foreach (SettingDefinition definition in _definitions)
                if (other.Contains(definition.Key))
                    CopyValueFrom(other, definition.Key);
        }

        public void CopyValueFrom(SettingsModel other, string key) {
            if (_toggleValues.ContainsKey(key))
                SetToggle(key, other.GetToggle(key));
            else if (_sliderValues.ContainsKey(key))
                SetSlider(key, other.GetSlider(key));
            else if (_choiceValues.ContainsKey(key))
                SetChoice(key, other.GetChoice(key));
        }

        public bool HasSameValue(SettingsModel other, string key) =>
            _toggleValues.TryGetValue(key, out bool toggle) ? toggle == other.GetToggle(key)
            : _sliderValues.TryGetValue(key, out float slider) ? Math.Abs(slider - other.GetSlider(key)) < 0.0001f
            : _choiceValues.TryGetValue(key, out string choice) && choice == other.GetChoice(key);

        public bool HasSameValues(SettingsModel other) {
            foreach (SettingDefinition definition in _definitions)
                if (HasSameValue(other, definition.Key) == false)
                    return false;

            return true;
        }

        private void AddDefinition(SettingDefinition definition) {
            if (definition == null)
                throw new ArgumentException("The settings catalog has an empty entry.");
            if (string.IsNullOrEmpty(definition.Key))
                throw new ArgumentException($"Setting '{definition.name}' has no key.");
            if (_definitionByKey.ContainsKey(definition.Key))
                throw new ArgumentException($"Two settings have the key '{definition.Key}'.");

            _definitionByKey.Add(definition.Key, definition);
            _definitions.Add(definition);
        }

        private TDefinition GetDefinition<TDefinition>(string key) where TDefinition : SettingDefinition =>
            GetDefinition(key) as TDefinition
            ?? throw new InvalidOperationException($"Setting '{key}' is not a {typeof(TDefinition).Name}.");

        private void SetValue<TValue>(Dictionary<string, TValue> values, string key, TValue value) {
            if (values.TryGetValue(key, out TValue current) && EqualityComparer<TValue>.Default.Equals(current, value))
                return;

            values[key] = value;
            OnValueChanged?.Invoke(key);
        }
    }
}
