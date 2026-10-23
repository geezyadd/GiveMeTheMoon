using System.Collections.Generic;
using Features.MvpModule;
using Features.StagePresetModule.Data;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
using Game.Connection;
using Mirror;

namespace Features.StagePresetModule.Scripts.UI {
    // The host's stage preset choice in the lobby, before Start. Option 0 is a normal run without a preset.
    public sealed class StagePresetPickerPresenter : PresenterBehaviour<StagePresetPickerViewBase> {
        private const string NO_PRESET_LABEL = "Normal run";
        private const string NO_PRESET_DESCRIPTION = "No preset: loop 0, the usual money and station drops.";
        private const int NO_PRESET_OPTION = 0;
        private const int FIRST_PRESET_OPTION = 1;

        private readonly IStagePresetSelectionService _stagePresetSelectionService;
        private readonly IReadOnlyStagePresetSelectionModel _stagePresetSelectionModel;
        private readonly IConnectionSessionService _connectionSessionService;
        private readonly List<string> _labels = new List<string>();

        public StagePresetPickerPresenter(
            IStagePresetSelectionService stagePresetSelectionService,
            IReadOnlyStagePresetSelectionModel stagePresetSelectionModel,
            IConnectionSessionService connectionSessionService) {
            _stagePresetSelectionService = stagePresetSelectionService;
            _stagePresetSelectionModel = stagePresetSelectionModel;
            _connectionSessionService = connectionSessionService;
        }

        protected override void OnViewSet() {
            View.OnOptionClicked += OnOptionClicked;
            _stagePresetSelectionModel.OnSelectedPresetIdChanged += OnSelectedPresetIdChanged;
            _connectionSessionService.OnMapReady += OnMapReady;
            View.SetOptions(CollectLabels());
            OnSelectedPresetIdChanged();
            OnMapReady();
        }

        protected override void OnDisposed() {
            View.OnOptionClicked -= OnOptionClicked;
            _stagePresetSelectionModel.OnSelectedPresetIdChanged -= OnSelectedPresetIdChanged;
            _connectionSessionService.OnMapReady -= OnMapReady;
        }

        private IReadOnlyList<string> CollectLabels() {
            _labels.Clear();
            _labels.Add(NO_PRESET_LABEL);
            for (int i = 0; i < _stagePresetSelectionService.Presets.Count; i++)
                _labels.Add(_stagePresetSelectionService.Presets[i].DisplayName);

            return _labels;
        }

        private bool IsHostInLobby() =>
            NetworkServer.active && _connectionSessionService.IsInLobby;

        // Only the host picks, in the lobby, and only where the picker is available (editor / development builds).
        private void OnMapReady() =>
            View.SetVisible(_stagePresetSelectionService.IsPickerAvailable && IsHostInLobby());

        private void OnOptionClicked(int optionIndex) {
            if (IsHostInLobby() == false)
                return;

            if (optionIndex == NO_PRESET_OPTION) {
                _stagePresetSelectionService.Clear();
                return;
            }

            _stagePresetSelectionService.Select(_stagePresetSelectionService.Presets[optionIndex - FIRST_PRESET_OPTION].Id);
        }

        private void OnSelectedPresetIdChanged() {
            if (_stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) == false) {
                View.SetSelected(NO_PRESET_OPTION, NO_PRESET_DESCRIPTION);
                return;
            }

            int presetIndex = 0;
            while (_stagePresetSelectionService.Presets[presetIndex] != preset)
                presetIndex++;

            View.SetSelected(presetIndex + FIRST_PRESET_OPTION, preset.Description);
        }
    }
}
