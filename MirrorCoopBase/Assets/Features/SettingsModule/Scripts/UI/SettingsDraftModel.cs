using Features.SettingsModule.Data;

namespace Features.SettingsModule.UI {
    // The values the settings window edits before Apply: a second settings model next to the live one, so the
    // appliers see nothing until the player applies.
    public sealed class SettingsDraftModel {
        public SettingsModel Values { get; }

        public SettingsDraftModel(SettingsCatalog catalog) =>
            Values = new SettingsModel(catalog);
    }
}
