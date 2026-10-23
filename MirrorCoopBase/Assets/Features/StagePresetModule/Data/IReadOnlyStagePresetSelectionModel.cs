using System;

namespace Features.StagePresetModule.Data {
    public interface IReadOnlyStagePresetSelectionModel {
        // Empty: no preset, the run starts as a normal game.
        public string SelectedPresetId { get; }

        public event Action OnSelectedPresetIdChanged;
    }
}
