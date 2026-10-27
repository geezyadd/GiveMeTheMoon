using System;
using System.Threading.Tasks;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Game.Connection;
using UnityEngine;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // A session that ended without the local player's Leave (the host turned the join away, quit or the connection
    // dropped) leaves the client in the menu scene but still in the session state: switch to the menu state so the menu
    // window opens (and shows the reason of a rejection). Join enters the session state right after connecting, so the
    // drop can come while that switch is still running.
    public sealed class SessionDroppedMenuSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private bool _isMenuPending;

        public SessionDroppedMenuSystem(
            ConnectionNetworkEvents connectionNetworkEvents,
            IGameFlowStateMachineService gameFlowStateMachineService) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _gameFlowStateMachineService = gameFlowStateMachineService;
        }

        public void Initialize() {
            _connectionNetworkEvents.SessionDropped += OnSessionDropped;
            _gameFlowStateMachineService.StateEntered += OnStateEntered;
        }

        public void Dispose() {
            _connectionNetworkEvents.SessionDropped -= OnSessionDropped;
            _gameFlowStateMachineService.StateEntered -= OnStateEntered;
        }

        private void TryEnterMenu() {
            if (_isMenuPending == false || _gameFlowStateMachineService.IsTransitioning)
                return;

            _isMenuPending = false;
            if (_gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState))
                _ = EnterMenuAsync();
        }

        private async Task EnterMenuAsync() {
            try {
                await _gameFlowStateMachineService.EnterAsync<MenuGameFlowState>();
            }
            catch (Exception exception) {
                Debug.LogException(exception);
            }
        }

        private void OnSessionDropped() {
            _isMenuPending = true;
            TryEnterMenu();
        }

        private void OnStateEntered(Type stateType) =>
            TryEnterMenu();
    }
}
