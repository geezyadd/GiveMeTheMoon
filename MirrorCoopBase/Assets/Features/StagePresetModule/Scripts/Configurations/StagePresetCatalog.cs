using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.StagePresetModule.Scripts.Configurations {
    // Every stage preset the lobby offers; a new preset is a new asset in this list.
    [CreateAssetMenu(
        fileName = nameof(StagePresetCatalog) + "_Default",
        menuName = "Configurations/StagePresetModule/" + nameof(StagePresetCatalog))]
    public sealed class StagePresetCatalog : ScriptableObject {
        [SerializeField] private StagePreset[] _presets = Array.Empty<StagePreset>();
        [Tooltip("Editor and development builds always show the lobby preset picker; a release build only with this on.")]
        [SerializeField] private bool _isPickerShownInReleaseBuilds;

        public IReadOnlyList<StagePreset> Presets => _presets;
        public bool IsPickerShownInReleaseBuilds => _isPickerShownInReleaseBuilds;
    }
}
