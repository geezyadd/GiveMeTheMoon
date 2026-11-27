using System.Collections.Generic;
using UnityEngine;

namespace Features.SettingsModule.Data {
    // Every player setting the game has. The settings model holds one value for each of them.
    [CreateAssetMenu(
        fileName = nameof(SettingsCatalog) + "_Default",
        menuName = "Configurations/SettingsModule/" + nameof(SettingsCatalog))]
    public sealed class SettingsCatalog : ScriptableObject {
        [SerializeField] private List<SettingDefinition> _settings = new();

        public IReadOnlyList<SettingDefinition> Settings => _settings;
    }
}
