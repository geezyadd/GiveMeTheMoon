using System;

namespace Features.SpaceHazeModule.Data {
    // This peer's own haze settings from the debug window: off, or a clear distance other than the configuration's.
    // Local and unsynced; the default is the configuration's haze.
    public sealed class SpaceHazeModel {
        public bool IsEnabled { get; private set; } = true;
        public bool HasClearMetersOverride { get; private set; }
        public float ClearMetersOverride { get; private set; }

        public event Action OnChanged;

        public void SetEnabled(bool isEnabled) {
            if (IsEnabled == isEnabled)
                return;

            IsEnabled = isEnabled;
            OnChanged?.Invoke();
        }

        public void SetClearMetersOverride(float clearMeters) {
            HasClearMetersOverride = true;
            ClearMetersOverride = clearMeters;
            OnChanged?.Invoke();
        }
    }
}
