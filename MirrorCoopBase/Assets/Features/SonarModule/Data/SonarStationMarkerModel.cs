using System;

namespace Features.SonarModule.Data {
    // Whether this peer's screen shows the next station's world marker; local, never synced. Only the debug window
    // turns it on for now, so a pilot playing solo sees where to fly without a navigator.
    public sealed class SonarStationMarkerModel {
        public bool IsShown { get; private set; }

        public event Action OnIsShownChanged;

        public void SetShown(bool isShown) {
            if (IsShown == isShown)
                return;

            IsShown = isShown;
            OnIsShownChanged?.Invoke();
        }
    }
}
