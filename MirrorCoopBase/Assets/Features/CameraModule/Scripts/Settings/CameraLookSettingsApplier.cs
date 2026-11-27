using System;
using Features.CameraModule.Scripts.Models;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.CameraModule.Scripts.Settings {
    // Mouse sensitivity and invert Y from the player's settings into the look model.
    public sealed class CameraLookSettingsApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly CameraLookSettingsModel _lookSettingsModel;

        public CameraLookSettingsApplier(SettingsModel settingsModel, CameraLookSettingsModel lookSettingsModel) {
            _settingsModel = settingsModel;
            _lookSettingsModel = lookSettingsModel;
        }

        public void Initialize() {
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            if (key == SettingKeys.MOUSE_SENSITIVITY || key == SettingKeys.INVERT_Y)
                Apply();
        }

        private void Apply() {
            _lookSettingsModel.LookSensitivity = _settingsModel.GetSlider(SettingKeys.MOUSE_SENSITIVITY);
            _lookSettingsModel.InvertY = _settingsModel.GetToggle(SettingKeys.INVERT_Y);
        }
    }
}
