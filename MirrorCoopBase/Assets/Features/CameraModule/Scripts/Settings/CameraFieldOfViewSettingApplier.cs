using System;
using Features.CameraModule.Scripts.Models;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.CameraModule.Scripts.Settings {
    // The field of view from the player's settings into the camera's field of view model.
    public sealed class CameraFieldOfViewSettingApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly CameraFieldOfViewModel _fieldOfViewModel;

        public CameraFieldOfViewSettingApplier(SettingsModel settingsModel, CameraFieldOfViewModel fieldOfViewModel) {
            _settingsModel = settingsModel;
            _fieldOfViewModel = fieldOfViewModel;
        }

        public void Initialize() {
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            if (key == SettingKeys.FIELD_OF_VIEW)
                Apply();
        }

        private void Apply() =>
            _fieldOfViewModel.FirstPersonFieldOfView = _settingsModel.GetSlider(SettingKeys.FIELD_OF_VIEW);
    }
}
