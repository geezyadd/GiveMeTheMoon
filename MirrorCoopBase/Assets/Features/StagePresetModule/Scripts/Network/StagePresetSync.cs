using Features.StagePresetModule.Data;
using Features.StagePresetModule.Data.Generated;
using Mirror;
using Zenject;

namespace Features.StagePresetModule.Scripts.Network {
    // Stands in the lobby and the game scene: carries the server's preset choice to every client and late joiner.
    public sealed class StagePresetSync : RunStagePresetBridge {
        private IReadOnlyStagePresetSelectionModel _stagePresetSelectionModel;

        [Inject]
        private void InjectDependencies(IReadOnlyStagePresetSelectionModel stagePresetSelectionModel) =>
            _stagePresetSelectionModel = stagePresetSelectionModel;

        public override void OnStartServer() {
            base.OnStartServer();
            _stagePresetSelectionModel.OnSelectedPresetIdChanged += OnSelectedPresetIdChanged;
            WritePreset();
        }

        public override void OnStopServer() {
            _stagePresetSelectionModel.OnSelectedPresetIdChanged -= OnSelectedPresetIdChanged;
            base.OnStopServer();
        }

        [Server]
        private void WritePreset() =>
            ServerSetPresetId(_stagePresetSelectionModel.SelectedPresetId);

        private void OnSelectedPresetIdChanged() =>
            WritePreset();
    }
}
