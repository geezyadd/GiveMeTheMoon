using System.Collections.Generic;
using UnityEngine;

namespace Features.SettingsModule.Data {
    // A choice out of a fixed list of options, in the order the settings window shows them.
    [CreateAssetMenu(
        fileName = nameof(ChoiceSettingDefinition),
        menuName = "Configurations/SettingsModule/" + nameof(ChoiceSettingDefinition))]
    public sealed class ChoiceSettingDefinition : ChoiceSettingDefinitionBase {
        [SerializeField] private List<SettingChoiceOption> _options = new();

        public IReadOnlyList<SettingChoiceOption> Options => _options;

        public override IReadOnlyList<SettingChoiceOption> GetOptions() =>
            _options;

        public override bool AcceptsOption(string optionId) {
            foreach (SettingChoiceOption option in _options)
                if (option.Id == optionId)
                    return true;

            return false;
        }
    }
}
