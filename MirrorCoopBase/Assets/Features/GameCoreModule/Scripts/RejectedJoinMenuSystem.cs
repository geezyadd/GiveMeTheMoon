using System;
using System.Threading.Tasks;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Game.Connection;
using UnityEngine;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // A player the host turned away is back in the menu scene but still in the session state: switch to the menu
    // state so the menu window opens and shows why. Join enters the session state right after connecting, so the
    // rejection can come back while that switch is still running.
    public sealed class RejectedJoinMenuSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly ConnectionRejectModel _connectionRejectModel;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private bool _isMenuPending;

        public RejectedJoinMenuSystem(
            ConnectionNetworkEvents connectionNetworkEvents,
            ConnectionRejectModel connectionRejectModel,
            IGameFlowStateMachineService gameFlowStateMachineService) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _connectionRejectModel = connectionRejectModel;
            _gameFlowStateMachineService = gameFlowStateMachineService;
        }

        public void Initialize() {
            _connectionNetworkEvents.ReturnedToMenu += OnReturnedToMenu;
            _gameFlowStateMachineService.StateEntered += OnStateEntered;
        }

        public void Dispose() {
            _connectionNetworkEvents.ReturnedToMenu -= OnReturnedToMenu;
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

        private void OnReturnedToMenu() {
            if (_connectionRejectModel.HasRejection == false)
                return;

            _isMenuPending = true;
            TryEnterMenu();
        }

        private void OnStateEntered(Type stateType) =>
            TryEnterMenu();
    }
}
