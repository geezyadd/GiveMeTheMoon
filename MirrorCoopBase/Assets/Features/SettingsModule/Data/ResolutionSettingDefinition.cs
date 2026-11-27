using UnityEngine;

namespace Features.SettingsModule.Data {
    // The screen resolution. Its options are not data: they are the resolutions the display supports, built at
    // runtime. The stored id is "<width>x<height>"; an empty id (the default) keeps the resolution the game started
    // with. Whether an id is still supported is checked by the module that applies it.
    [CreateAssetMenu(
        fileName = nameof(ResolutionSettingDefinition),
        menuName = "Configurations/SettingsModule/" + nameof(ResolutionSettingDefinition))]
    public sealed class ResolutionSettingDefinition : ChoiceSettingDefinitionBase {
        public override bool AcceptsOption(string optionId) =>
            optionId != null;
    }
}
