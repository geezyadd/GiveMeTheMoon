using System;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.MvpModule;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    public sealed class GameFlowWindowsSystem : IInitializable, IDisposable {
        private readonly IGameFlowStateMachineService _gameFlow;
        private readonly IWindowsService _windowsService;

        public GameFlowWindowsSystem(
            IGameFlowStateMachineService gameFlow,
            IWindowsService windowsService) {
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
            _windowsService = windowsService ?? throw new ArgumentNullException(nameof(windowsService));
        }

        public void Initialize() {
            _gameFlow.StateEntered += OnStateEntered;
        }

        public void Dispose() {
            _gameFlow.StateEntered -= OnStateEntered;
        }

        void OnStateEntered(Type stateType) {
            if (stateType == typeof(MenuGameFlowState)) {
                CloseIfOpen<GameHudWindow>();
                OpenOrShow<MenuWindow>();
                return;
            }

            if (stateType == typeof(SessionGameFlowState)) {
                CloseIfOpen<MenuWindow>();
                OpenOrShow<GameHudWindow>();
            }
        }

        void OpenOrShow<TWindow>() where TWindow : IWindow {
            IWindow window = _windowsService.GetWindow(typeof(TWindow));
            if (window == null)
                return;

            if (window.WindowStatus == WindowStatus.Closed)
                _windowsService.OpenWindow<TWindow>();
            else if (window.WindowStatus == WindowStatus.Hidden)
                _windowsService.ShowWindow<TWindow>();
        }

        void CloseIfOpen<TWindow>() where TWindow : IWindow {
            IWindow window = _windowsService.GetWindow(typeof(TWindow));
            if (window == null || window.WindowStatus == WindowStatus.Closed)
                return;

            _windowsService.CloseWindow<TWindow>();
        }
    }
}
