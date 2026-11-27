using System;
using System.Collections.Generic;
using Features.SavingSystemModule.Contracts;
using Features.SettingsModule.Data;
using UnityEngine;

namespace Features.SettingsModule.Saving {
    // Writes the settings model into the save service and reads it back. A value that no longer fits (a removed
    // setting, another kind, an option that is gone) is skipped and keeps its default; a save that cannot be read
    // leaves every default.
    public sealed class SettingsSaveStore {
        public const string SAVE_ID = "settings";
        public const string SAVE_FILE = "settings";

        private readonly ISaveService _saveService;

        public SettingsSaveStore(ISaveService saveService) =>
            _saveService = saveService;

        public void Load(SettingsModel model) {
            SettingsSaveData data;
            try {
                if (_saveService.TryLoad(SAVE_ID, out data) == false || data == null)
                    return;
            }
            catch (Exception exception) {
                Debug.LogWarning($"Settings: the saved settings could not be read, the defaults stay. {exception.Message}");
                return;
            }

            Apply(data.Toggles, (key, value) => model.SetToggle(key, value));
            Apply(data.Sliders, (key, value) => model.SetSlider(key, value));
            Apply(data.Choices, (key, value) => model.SetChoice(key, value));
        }

        public void Save(SettingsModel model) {
            SettingsSaveData data = new() {
                Toggles = new Dictionary<string, bool>(model.ToggleValues),
                Sliders = new Dictionary<string, float>(model.SliderValues),
                Choices = new Dictionary<string, string>(model.ChoiceValues)
            };
            _saveService.Save(SAVE_ID, data, SAVE_FILE);
            _saveService.Commit(SAVE_FILE);
        }

        private static void Apply<TValue>(Dictionary<string, TValue> values, Action<string, TValue> set) {
            if (values == null)
                return;

            foreach (KeyValuePair<string, TValue> pair in values) {
                try {
                    set(pair.Key, pair.Value);
                }
                catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException
                                                      or ArgumentException) { }
            }
        }
    }
}
