using UnityEngine;

namespace Features.SettingsModule.Data {
    // One player setting: its key, its label in the settings window and its default value (in the kind's subclass).
    public abstract class SettingDefinition : ScriptableObject {
        [SerializeField] private string _key;
        [SerializeField] private string _label;
        [Tooltip("The settings window applies and saves a change at once, without Apply (the volumes).")]
        [SerializeField] private bool _appliesInstantly;

        public string Key => _key;
        public string Label => _label;
        public bool AppliesInstantly => _appliesInstantly;

        // Writes this setting's default value into the model.
        public abstract void WriteDefault(SettingsModel model);
    }
}
