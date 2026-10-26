using System;
using Features.CameraModule.Scripts.Models;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using Zenject;

namespace Features.DebugModule.Scripts.Systems {
    // The debug window (the corner button) is there for the whole session. The open panel frees the cursor, so a
    // player can click it while walking or seated at a station; it does not stop the walking. Esc closes it.
    public sealed class DebugWindowSystem : IInitializable, IDisposable {
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly IWindowsService _windowsService;
        private readonly IInputService _inputService;
        private readonly DebugPanelModel _debugPanelModel;
        private readonly CursorModel _cursorModel;

        private bool _isCursorRequested;

        public DebugWindowSystem(
            IGameFlowStateMachineService gameFlowStateMachineService,
            IWindowsService windowsService,
            IInputService inputService,
            DebugPanelModel debugPanelModel,
            CursorModel cursorModel) {
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _windowsService = windowsService;
            _inputService = inputService;
            _debugPanelModel = debugPanelModel;
            _cursorModel = cursorModel;
        }

        public void Initialize() {
            _gameFlowStateMachineService.StateEntered += OnGameFlowStateEntered;
            _debugPanelModel.OnOpenChanged += OnPanelOpenChanged;
            _inputService.CloseWindow.Performed += OnCloseKeyPressed;
        }

        public void Dispose() {
            _debugPanelModel.Close();
            _gameFlowStateMachineService.StateEntered -= OnGameFlowStateEntered;
            _debugPanelModel.OnOpenChanged -= OnPanelOpenChanged;
            _inputService.CloseWindow.Performed -= OnCloseKeyPressed;
        }

        private void OpenWindow() {
            if (_windowsService.GetWindow(typeof(DebugWindow)).WindowStatus == WindowStatus.Closed)
                _windowsService.OpenWindow<DebugWindow>();
        }

        private void CloseWindow() {
            _debugPanelModel.Close();
            if (_windowsService.GetWindow(typeof(DebugWindow)).WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<DebugWindow>();
        }

        private void RequestCursor() {
            if (_isCursorRequested)
                return;

            _isCursorRequested = true;
            _cursorModel.RequestFreeCursor();
        }

        private void ReleaseCursor() {
            if (_isCursorRequested == false)
                return;

            _isCursorRequested = false;
            _cursorModel.ReleaseFreeCursor();
        }

        private void OnGameFlowStateEntered(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                OpenWindow();
            else
                CloseWindow();
        }

        private void OnPanelOpenChanged() {
            if (_debugPanelModel.IsOpen)
                RequestCursor();
            else
                ReleaseCursor();
        }

        private void OnCloseKeyPressed() =>
            _debugPanelModel.Close();
    }
}
