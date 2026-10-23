using System.Collections.Generic;
using Features.StagePresetModule.Data;
using Features.StagePresetModule.Scripts.Configurations;
using UnityEngine;

namespace Features.StagePresetModule.Scripts.Core {
    public sealed class StagePresetSelectionService : IStagePresetSelectionService {
        private const string UNKNOWN_PRESET_FORMAT = "{0}: no stage preset '{1}'.";

        private readonly StagePresetCatalog _stagePresetCatalog;
        private readonly StagePresetSelectionModel _stagePresetSelectionModel;

        public bool IsPickerAvailable {
            get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return _stagePresetCatalog.IsPickerShownInReleaseBuilds;
#endif
            }
        }

        public IReadOnlyList<StagePreset> Presets =>
            _stagePresetCatalog.Presets;

        public StagePresetSelectionService(StagePresetCatalog stagePresetCatalog, StagePresetSelectionModel stagePresetSelectionModel) {
            _stagePresetCatalog = stagePresetCatalog;
            _stagePresetSelectionModel = stagePresetSelectionModel;
        }

        public bool TryGetPreset(string presetId, out StagePreset preset) {
            preset = null;
            if (string.IsNullOrEmpty(presetId))
                return false;

            for (int i = 0; i < _stagePresetCatalog.Presets.Count; i++) {
                if (_stagePresetCatalog.Presets[i].Id != presetId)
                    continue;

                preset = _stagePresetCatalog.Presets[i];
                return true;
            }

            return false;
        }

        public bool TryGetSelectedPreset(out StagePreset preset) =>
            TryGetPreset(_stagePresetSelectionModel.SelectedPresetId, out preset);

        public bool TryGetSelectedExtension<T>(out T extension) where T : ScriptableObject {
            extension = null;
            if (TryGetSelectedPreset(out StagePreset preset) == false)
                return false;

            for (int i = 0; i < preset.Extensions.Count; i++) {
                if (preset.Extensions[i] is not T block)
                    continue;

                extension = block;
                return true;
            }

            return false;
        }

        public void Select(string presetId) {
            if (string.IsNullOrEmpty(presetId) == false && TryGetPreset(presetId, out _) == false)
                throw new KeyNotFoundException(string.Format(UNKNOWN_PRESET_FORMAT, _stagePresetCatalog.name, presetId));

            _stagePresetSelectionModel.Select(presetId ?? string.Empty);
        }

        public void Clear() =>
            _stagePresetSelectionModel.Clear();
    }
}
