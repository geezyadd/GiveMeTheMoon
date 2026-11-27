using System.Collections.Generic;
using UnityEngine;

namespace Features.SettingsModule.Data {
    // The screen resolution. Its options are not data: they are the resolutions the display supports, built at
    // runtime. The stored id is "<width>x<height>"; an empty id (the default) keeps the resolution the game started
    // with. Whether an id is still supported is checked by the module that applies it.
    [CreateAssetMenu(
        fileName = nameof(ResolutionSettingDefinition),
        menuName = "Configurations/SettingsModule/" + nameof(ResolutionSettingDefinition))]
    public sealed class ResolutionSettingDefinition : ChoiceSettingDefinitionBase {
        [Tooltip("string.Format pattern of an option's label: {0} is the width, {1} the height.")]
        [SerializeField] private string _labelFormat = "{0} x {1}";

        public override bool AcceptsOption(string optionId) =>
            optionId != null;

        // The display's resolutions, each width x height once, smallest first.
        public override IReadOnlyList<SettingChoiceOption> GetOptions() {
            List<SettingChoiceOption> options = new();
            HashSet<string> seen = new();
            foreach (Resolution resolution in Screen.resolutions) {
                string id = ResolutionOptionId.Format(resolution.width, resolution.height);
                if (seen.Add(id))
                    options.Add(new SettingChoiceOption(id, string.Format(_labelFormat, resolution.width, resolution.height)));
            }

            string current = ResolveShownOptionId(string.Empty);
            if (seen.Contains(current) == false)
                options.Add(new SettingChoiceOption(current, string.Format(_labelFormat, Screen.width, Screen.height)));

            return options;
        }

        // The empty default shows as the resolution the screen has now.
        public override string ResolveShownOptionId(string optionId) =>
            string.IsNullOrEmpty(optionId) ? ResolutionOptionId.Format(Screen.width, Screen.height) : optionId;
    }
}
