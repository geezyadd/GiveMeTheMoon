using System.Collections.Generic;
using Features.StagePresetModule.Scripts.Configurations;
using UnityEngine;

namespace Features.StagePresetModule.Scripts.Core {
    public interface IStagePresetSelectionService {
        public bool IsPickerAvailable { get; }
        public IReadOnlyList<StagePreset> Presets { get; }
        public bool TryGetPreset(string presetId, out StagePreset preset);
        public bool TryGetSelectedPreset(out StagePreset preset);
        // A system reads its own optional settings block of the selected preset.
        public bool TryGetSelectedExtension<T>(out T extension) where T : ScriptableObject;
        // Empty id: no preset.
        public void Select(string presetId);
        public void Clear();
    }
}
