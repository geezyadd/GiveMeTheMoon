using System;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using UnityEngine;
using Zenject;

namespace Features.LobbyModule.Scripts.Pause {
    // Esc in the lobby and the game: an open window (shop, sonar, emote wheel, settings...) closes first through its
    // own system; only when no such window was open does Esc open or close the pause menu. Windows that stay open the
    // whole session (IBackgroundWindow: the HUD, the debug overlay) do not count. Every system hears the same press, so
    // a window closed by this very press (in this frame) counts as open. The pause menu closes when the session ends.
    public sealed class PauseMenuEscapeSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly IWindowsService _windowsService;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly IPauseMenuService _pauseMenuService;

        private int _otherWindowClosedFrame = -1;

        public PauseMenuEscapeSystem(
            IInputService inputService,
            IWindowsService windowsService,
            IGameFlowStateMachineService gameFlowStateMachineService,
            IPauseMenuService pauseMenuService) {
            _inputService = inputService;
            _windowsService = windowsService;
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _pauseMenuService = pauseMenuService;
        }

        public void Initialize() {
            _inputService.CloseWindow.Performed += OnCloseWindowPressed;
            _windowsService.OnWindowClosed += OnWindowClosed;
            _gameFlowStateMachineService.StateExited += OnStateExited;
        }

        public void Dispose() {
            _inputService.CloseWindow.Performed -= OnCloseWindowPressed;
            _windowsService.OnWindowClosed -= OnWindowClosed;
            _gameFlowStateMachineService.StateExited -= OnStateExited;
        }

        private void OnCloseWindowPressed() {
            if (IsHudShown() == false || IsOtherWindowInTheWay())
                return;

            if (_pauseMenuService.IsOpen)
                _pauseMenuService.Close();
            else
                _pauseMenuService.Open();
        }

        private void OnWindowClosed(Type windowType) {
            if (IsInTheWay(windowType))
                _otherWindowClosedFrame = Time.frameCount;
        }

        private void OnStateExited(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _pauseMenuService.Close();
        }

        private bool IsHudShown() =>
            _windowsService.GetWindow(typeof(GameHudWindow)) is { WindowStatus: WindowStatus.Showed };

        private bool IsOtherWindowInTheWay() {
            if (_otherWindowClosedFrame == Time.frameCount)
                return true;

            foreach (IWindow window in _windowsService.GetAllWindowsWithStatus(WindowStatus.Showed))
                if (IsInTheWay(window.GetType()))
                    return true;

            return false;
        }

        private static bool IsInTheWay(Type windowType) =>
            windowType != typeof(PauseMenuWindow) && typeof(IBackgroundWindow).IsAssignableFrom(windowType) == false;
    }
}
