using UnityEngine;

namespace Features.SettingsModule.Data {
    // A setting whose value is one option out of several, stored as the option's id.
    public abstract class ChoiceSettingDefinitionBase : SettingDefinition {
        [SerializeField] private string _defaultOptionId;

        public string DefaultOptionId => _defaultOptionId;

        public abstract bool AcceptsOption(string optionId);

        public override void WriteDefault(SettingsModel model) =>
            model.SetChoice(Key, _defaultOptionId);
    }
}
