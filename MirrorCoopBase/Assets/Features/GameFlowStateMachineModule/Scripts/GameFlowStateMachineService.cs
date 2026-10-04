using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Features.GameFlowStateMachineModule.Scripts.States;

namespace Features.GameFlowStateMachineModule.Scripts {
    public sealed class GameFlowStateMachineService : IGameFlowStateMachineService {
        private readonly IReadOnlyDictionary<Type, IGameFlowState> _states;
        private readonly GameFlowSceneSwitcher _sceneSwitcher;
        private IGameFlowState _currentState;

        public GameFlowStateMachineService(
            List<IGameFlowState> states,
            GameFlowSceneSwitcher sceneSwitcher) {
            _states = states.ToDictionary(state => state.GetType());
            _sceneSwitcher = sceneSwitcher;
        }

        public event Action<Type> StateEntered;
        public event Action<Type> StateExited;

        public Type CurrentStateType => _currentState?.GetType();
        public bool IsTransitioning { get; private set; }

        public async Task EnterAsync<TState>() where TState : class, IGameFlowState {
            Type nextStateType = typeof(TState);

            if (IsTransitioning)
                throw new InvalidOperationException("A GameFlow state transition is already in progress.");

            if (CurrentStateType == nextStateType)
                return;

            if (IsTransitionAllowed(CurrentStateType, nextStateType) == false)
                throw new InvalidOperationException(
                    $"GameFlow transition from {CurrentStateType?.Name ?? "None"} to {nextStateType.Name} is not allowed.");

            if (_states.TryGetValue(nextStateType, out IGameFlowState nextState) == false)
                throw new InvalidOperationException($"GameFlow state {nextStateType.Name} is not registered.");

            IsTransitioning = true;
            Type previousStateType = CurrentStateType;

            try {
                if (_currentState != null) {
                    await _currentState.ExitAsync();
                    StateExited?.Invoke(previousStateType);
                }

                await _sceneSwitcher.SwitchAsync(previousStateType, nextStateType);
                await nextState.EnterAsync();
                _currentState = nextState;
                StateEntered?.Invoke(nextStateType);
            }
            finally {
                IsTransitioning = false;
            }
        }

        private static bool IsTransitionAllowed(Type currentStateType, Type nextStateType) {
            if (currentStateType == null)
                return nextStateType == typeof(GlobalGameFlowState);

            if (currentStateType == typeof(GlobalGameFlowState))
                return nextStateType == typeof(MenuGameFlowState);

            if (currentStateType == typeof(MenuGameFlowState))
                return nextStateType == typeof(SessionGameFlowState);

            if (currentStateType == typeof(SessionGameFlowState))
                return nextStateType == typeof(MenuGameFlowState);

            return false;
        }
    }
}
