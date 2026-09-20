using System;
using System.Collections.Generic;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    public sealed class GameplaySessionLifecycle : IInitializable, IDisposable {
        private readonly List<IGameplaySession> _sessions;
        private readonly GameFlowStateLifecycleEventClass _lifecycle;

        public GameplaySessionLifecycle(
            List<IGameplaySession> sessions,
            GameFlowStateLifecycleEventClass lifecycle) {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public void Initialize() {
            _lifecycle.OnStateEntered += OnStateEntered;
        }

        public void Dispose() {
            _lifecycle.OnStateEntered -= OnStateEntered;
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
