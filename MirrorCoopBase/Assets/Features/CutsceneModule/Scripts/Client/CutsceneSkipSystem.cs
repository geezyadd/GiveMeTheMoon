using System;
using Features.CutsceneModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Network;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts.Contracts;
using Zenject;

namespace Features.CutsceneModule.Scripts.Client {
    // Every peer: Space (the jump button, which the cutscene's control block keeps from jumping) asks the server to
    // skip the playing cutscene; the server skips it for everyone.
    public sealed class CutsceneSkipSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly CutsceneLocalModel _cutsceneLocalModel;
        private readonly IReadOnlyComponentRegistry<CutsceneSync> _cutsceneSyncRegistry;

        public CutsceneSkipSystem(IInputService inputService, CutsceneLocalModel cutsceneLocalModel, IReadOnlyComponentRegistry<CutsceneSync> cutsceneSyncRegistry) {
            _inputService = inputService;
            _cutsceneLocalModel = cutsceneLocalModel;
            _cutsceneSyncRegistry = cutsceneSyncRegistry;
        }

        public void Initialize() =>
            _inputService.Jump.Performed += OnSkipPressed;

        public void Dispose() =>
            _inputService.Jump.Performed -= OnSkipPressed;

        private void OnSkipPressed() {
            if (_cutsceneLocalModel.IsPlaying && _cutsceneSyncRegistry.Items.Count > 0)
                _cutsceneSyncRegistry.Items[0].RequestSkip();
        }
    }
}
