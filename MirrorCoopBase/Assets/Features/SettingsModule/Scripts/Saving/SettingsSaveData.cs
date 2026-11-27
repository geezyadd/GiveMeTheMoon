using System.Collections.Generic;

namespace Features.SettingsModule.Saving {
    // The player's settings as they are saved: each kind's values by setting key.
    public sealed class SettingsSaveData {
        public Dictionary<string, bool> Toggles = new();
        public Dictionary<string, float> Sliders = new();
        public Dictionary<string, string> Choices = new();
    }
}
