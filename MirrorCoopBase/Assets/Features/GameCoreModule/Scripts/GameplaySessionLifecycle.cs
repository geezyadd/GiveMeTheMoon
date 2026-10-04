using System;
using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    public sealed class GameplaySessionLifecycle : IInitializable, IDisposable {
        private readonly List<IGameplaySession> _sessions;
        private readonly IGameFlowStateMachineService _gameFlow;

        public GameplaySessionLifecycle(
            List<IGameplaySession> sessions,
            IGameFlowStateMachineService gameFlow) {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
        }

        public void Initialize() {
            _gameFlow.StateEntered += OnStateEntered;
        }

        public void Dispose() {
            _gameFlow.StateEntered -= OnStateEntered;
        }

        private void OnStateEntered(Type stateType) {
            if (stateType == typeof(MenuGameFlowState)) {
                for (int i = 0; i < _sessions.Count; i++)
                    _sessions[i].CleanupGameplay();
                return;
            }

            if (stateType == typeof(SessionGameFlowState)) {
                for (int i = 0; i < _sessions.Count; i++)
                    _sessions[i].RestartGameplay();
            }
        }
    }
}
