using System;
using Features.SettingsModule.Data;
using Zenject;

namespace Features.AudioModule.Volume {
    // The volume settings into the FMOD mixer's buses: each slider (0–100 %) is its bus's volume (0–1). The window
    // applies the volumes at once (their definitions apply instantly), so the player hears the change while dragging.
    public sealed class AudioVolumeSettingsApplier : IInitializable, IDisposable {
        // The setting key and the FMOD Studio bus it sets. Ambiance has no slider of its own; it follows All.
        private static readonly (string Key, string BusPath)[] VolumeBuses = {
            (SettingKeys.MASTER_VOLUME, "bus:/All"),
            (SettingKeys.MUSIC_VOLUME, "bus:/All/Music"),
            (SettingKeys.SFX_VOLUME, "bus:/All/Sfx"),
        };

        private const float PERCENT = 100f;

        private readonly SettingsModel _settingsModel;
        private readonly IAudioService _audioService;

        public AudioVolumeSettingsApplier(SettingsModel settingsModel, IAudioService audioService) {
            _settingsModel = settingsModel;
            _audioService = audioService;
        }

        public void Initialize() {
            foreach ((string key, string busPath) in VolumeBuses)
                Apply(key, busPath);

            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() =>
            _settingsModel.OnValueChanged -= OnValueChanged;

        private void OnValueChanged(string key) {
            foreach ((string volumeKey, string busPath) in VolumeBuses)
                if (volumeKey == key)
                    Apply(volumeKey, busPath);
        }

        private void Apply(string key, string busPath) =>
            _audioService.SetBusVolume(busPath, _settingsModel.GetSlider(key) / PERCENT);
    }
}
