using UnityEngine;

namespace Features.SettingsModule.Data {
    // One player setting: its key, its label in the settings window and its default value (in the kind's subclass).
    public abstract class SettingDefinition : ScriptableObject {
        [SerializeField] private string _key;
        [SerializeField] private string _label;

        public string Key => _key;
        public string Label => _label;

        // Writes this setting's default value into the model.
        public abstract void WriteDefault(SettingsModel model);
    }
}
