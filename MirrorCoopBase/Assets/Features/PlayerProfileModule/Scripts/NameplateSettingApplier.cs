using System;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.PlayerProfileModule.Scripts {
    // The "show player names" setting into the nameplates' visibility model.
    public sealed class NameplateSettingApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly PlayerNameplateVisibilityModel _visibilityModel;

        public NameplateSettingApplier(SettingsModel settingsModel, PlayerNameplateVisibilityModel visibilityModel) {
            _settingsModel = settingsModel;
            _visibilityModel = visibilityModel;
        }

        public void Initialize() {
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            if (key == SettingKeys.NAMEPLATES)
                Apply();
        }

        private void Apply() =>
            _visibilityModel.AreShown = _settingsModel.GetToggle(SettingKeys.NAMEPLATES);
    }
}
