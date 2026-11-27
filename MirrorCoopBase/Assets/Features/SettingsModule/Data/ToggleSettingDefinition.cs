using UnityEngine;

namespace Features.SettingsModule.Data {
    // An on / off setting.
    [CreateAssetMenu(
        fileName = nameof(ToggleSettingDefinition),
        menuName = "Configurations/SettingsModule/" + nameof(ToggleSettingDefinition))]
    public sealed class ToggleSettingDefinition : SettingDefinition {
        [SerializeField] private bool _defaultValue;

        public bool DefaultValue => _defaultValue;

        public override void WriteDefault(SettingsModel model) =>
            model.SetToggle(Key, _defaultValue);
    }
}
