namespace Features.SettingsModule.Saving {
    // Saves the live settings (the settings window calls it after Apply and after a display change is kept or
    // reverted).
    public interface ISettingsSaving {
        public void Save();
    }
}
