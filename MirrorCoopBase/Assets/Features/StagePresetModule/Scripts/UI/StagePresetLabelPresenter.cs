using Features.MvpModule;
using Features.StagePresetModule.Data.Generated;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;

namespace Features.StagePresetModule.Scripts.UI {
    // "Preset: Late" for every player of a preset run, from the synced model (a late joiner gets it on spawn).
    public sealed class StagePresetLabelPresenter : PresenterBehaviour<StagePresetLabelViewBase> {
        private const string LABEL_FORMAT = "Preset: {0}";

        private readonly IReadOnlyRunStagePresetModel _runStagePresetModel;
        private readonly IStagePresetSelectionService _stagePresetSelectionService;

        public StagePresetLabelPresenter(
            IReadOnlyRunStagePresetModel runStagePresetModel,
            IStagePresetSelectionService stagePresetSelectionService) {
            _runStagePresetModel = runStagePresetModel;
            _stagePresetSelectionService = stagePresetSelectionService;
        }

        protected override void OnViewSet() {
            _runStagePresetModel.OnPresetIdChanged += OnPresetIdChanged;
            OnPresetIdChanged();
        }

        protected override void OnDisposed() =>
            _runStagePresetModel.OnPresetIdChanged -= OnPresetIdChanged;

        private void OnPresetIdChanged() {
            if (_stagePresetSelectionService.TryGetPreset(_runStagePresetModel.PresetId, out StagePreset preset) == false) {
                View.HidePreset();
                return;
            }

            View.ShowPreset(string.Format(LABEL_FORMAT, preset.DisplayName));
        }
    }
}
