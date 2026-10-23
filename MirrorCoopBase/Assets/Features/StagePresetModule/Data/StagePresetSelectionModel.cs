using System;

namespace Features.StagePresetModule.Data {
    // The server's choice of the next run's stage preset. It outlives the lobby and game scenes; the RunStagePreset
    // model carries it to the clients.
    public sealed class StagePresetSelectionModel : IReadOnlyStagePresetSelectionModel {
        public string SelectedPresetId { get; private set; } = string.Empty;

        public event Action OnSelectedPresetIdChanged;

        public void Select(string presetId) {
            if (SelectedPresetId == presetId)
                return;

            SelectedPresetId = presetId;
            OnSelectedPresetIdChanged?.Invoke();
        }

        public void Clear() =>
            Select(string.Empty);
    }
}
