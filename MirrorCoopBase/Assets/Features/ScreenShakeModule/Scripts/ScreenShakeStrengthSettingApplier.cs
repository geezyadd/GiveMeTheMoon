using System;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.ScreenShakeModule {
    // The screen shake setting into the screen shake's strength: the setting's default is today's strength (1x),
    // twice the default is 2x, 0 turns the shake off.
    public sealed class ScreenShakeStrengthSettingApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly ScreenShakeModel _screenShakeModel;

        public ScreenShakeStrengthSettingApplier(SettingsModel settingsModel, ScreenShakeModel screenShakeModel) {
            _settingsModel = settingsModel;
            _screenShakeModel = screenShakeModel;
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
            _screenShakeModel.StrengthScale = _settingsModel.GetSliderScaleToDefault(SettingKeys.SCREEN_SHAKE);
    }
}
