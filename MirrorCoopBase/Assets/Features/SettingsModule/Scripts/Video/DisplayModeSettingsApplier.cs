using System;
using Features.SettingsModule.Data;
using UnityEngine;
using Zenject;

namespace Features.SettingsModule.Video {
    // Window mode and resolution from the player's settings to the screen. An empty or no longer supported resolution
    // keeps the one the game started with.
    public sealed class DisplayModeSettingsApplier : IInitializable, IDisposable {
        private readonly SettingsModel _settingsModel;
        private readonly VideoSettingsConfiguration _configuration;

        private int _startupWidth;
        private int _startupHeight;

        public DisplayModeSettingsApplier(SettingsModel settingsModel, VideoSettingsConfiguration configuration) {
            _settingsModel = settingsModel;
            _configuration = configuration;
        }

        public void Initialize() {
            _startupWidth = Screen.width;
            _startupHeight = Screen.height;
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            if (key == SettingKeys.WINDOW_MODE || key == SettingKeys.RESOLUTION)
                Apply();
        }

        private void Apply() {
            if (_configuration.TryGetWindowMode(_settingsModel.GetChoice(SettingKeys.WINDOW_MODE), out FullScreenMode mode) == false)
                mode = Screen.fullScreenMode;

            if (ResolutionOptionId.TryParse(_settingsModel.GetChoice(SettingKeys.RESOLUTION), out int width, out int height) == false
                || IsSupported(width, height) == false) {
                width = _startupWidth;
                height = _startupHeight;
            }

            if (Screen.fullScreenMode == mode && Screen.width == width && Screen.height == height)
                return;

            Screen.SetResolution(width, height, mode);
        }

        private static bool IsSupported(int width, int height) {
            foreach (Resolution resolution in Screen.resolutions)
                if (resolution.width == width && resolution.height == height)
                    return true;

            return false;
        }
    }
}
