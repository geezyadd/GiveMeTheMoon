using System;
using Features.SettingsModule.Data;
using UnityEngine;
using Zenject;

namespace Features.SettingsModule.Video {
    // VSync and the FPS limit from the player's settings. The engine ignores the limit while VSync is on. The VSync
    // the project started with is restored on dispose, so a run in the editor does not change the project's quality
    // settings.
    public sealed class FrameRateSettingsApplier : IInitializable, IDisposable {
        private const int NO_FRAME_RATE_LIMIT = -1;

        private readonly SettingsModel _settingsModel;
        private readonly VideoSettingsConfiguration _configuration;

        private int _startupVSyncCount;
        private int _startupTargetFrameRate;

        public FrameRateSettingsApplier(SettingsModel settingsModel, VideoSettingsConfiguration configuration) {
            _settingsModel = settingsModel;
            _configuration = configuration;
        }

        public void Initialize() {
            _startupVSyncCount = QualitySettings.vSyncCount;
            _startupTargetFrameRate = Application.targetFrameRate;
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() {
            _settingsModel.OnValueChanged -= OnValueChanged;
            QualitySettings.vSyncCount = _startupVSyncCount;
            Application.targetFrameRate = _startupTargetFrameRate;
        }

        private void OnValueChanged(string key) {
            if (key == SettingKeys.VSYNC || key == SettingKeys.FPS_LIMIT)
                Apply();
        }

        private void Apply() {
            QualitySettings.vSyncCount = _settingsModel.GetToggle(SettingKeys.VSYNC) ? 1 : 0;
            Application.targetFrameRate = GetFrameRateLimit(_settingsModel.GetChoice(SettingKeys.FPS_LIMIT));
        }

        private int GetFrameRateLimit(string optionId) =>
            optionId != _configuration.UnlimitedFrameRateOptionId && int.TryParse(optionId, out int frameRate) && frameRate > 0
                ? frameRate
                : NO_FRAME_RATE_LIMIT;
    }
}
