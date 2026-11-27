using System;
using Features.AudioModule;
using Features.AudioModule.Ambience;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // Plays the session music from entering the session (it starts in the lobby) and stops it on leaving to the menu.
    // Local on each peer: every player hears it from their own game flow.
    public sealed class AmbienceSystem : IInitializable, IDisposable {
        private readonly IGameFlowStateMachineService _gameFlow;
        private readonly SoundInstance _music;

        public AmbienceSystem(IGameFlowStateMachineService gameFlow, IAudioService audioService, AmbienceConfiguration configuration) {
            _gameFlow = gameFlow;
            _music = audioService.CreateInstance(configuration.SessionMusic);
        }

        public void Initialize() {
            _gameFlow.StateEntered += OnStateEntered;
            _gameFlow.StateExited += OnStateExited;
        }

        public void Dispose() {
            _gameFlow.StateEntered -= OnStateEntered;
            _gameFlow.StateExited -= OnStateExited;
            _music.Dispose();
        }

        private void OnStateEntered(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _music.Start();
        }

        private void OnStateExited(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _music.Stop();
        }
    }
}
