using System.Collections.Generic;
using UnityEngine;

namespace Features.SettingsModule.Data {
    // A setting whose value is one option out of several, stored as the option's id.
    public abstract class ChoiceSettingDefinitionBase : SettingDefinition {
        [SerializeField] private string _defaultOptionId;

        public string DefaultOptionId => _defaultOptionId;

        public abstract bool AcceptsOption(string optionId);

        // The options the settings window cycles through, in order.
        public abstract IReadOnlyList<SettingChoiceOption> GetOptions();

        // The option the window shows for a stored value (a stored value may stand for another option, like the
        // resolution's empty default).
        public virtual string ResolveShownOptionId(string optionId) =>
            optionId;

        public override void WriteDefault(SettingsModel model) =>
            model.SetChoice(Key, _defaultOptionId);
    }
}
