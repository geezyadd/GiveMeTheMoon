using System;
using Features.CameraModule.Scripts.Shake;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.CameraModule.Scripts.Settings {
    // The screen shake setting into the camera shake's strength: the setting's default is today's strength (1x), twice
    // the default is 2x, 0 turns the shake off.
    public sealed class CameraShakeStrengthSettingApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly CameraShakeModel _cameraShakeModel;

        public CameraShakeStrengthSettingApplier(SettingsModel settingsModel, CameraShakeModel cameraShakeModel) {
            _settingsModel = settingsModel;
            _cameraShakeModel = cameraShakeModel;
        }

        public void Initialize() {
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            if (key == SettingKeys.SCREEN_SHAKE)
                Apply();
        }

        private void Apply() =>
            _cameraShakeModel.StrengthScale = _settingsModel.GetSliderScaleToDefault(SettingKeys.SCREEN_SHAKE);
    }
}
