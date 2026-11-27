using System;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts.Models;
using Features.EmoteModule.Data;
using Features.EmoteModule.UI;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using UnityEngine;
using Zenject;

namespace Features.EmoteModule {
    // The local player's emote wheel: holding the emote key opens it in the middle of the screen, the mouse moves its
    // pointer (the camera does not turn meanwhile), and releasing the key picks the highlighted emote; released in the
    // dead zone it picks nothing. It opens only in a session, while no window holds the controls or the cursor.
    public sealed class EmoteWheelSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly IWindowsService _windowsService;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly EmoteWheelModel _wheelModel;
        private readonly EmoteCatalog _catalog;
        private readonly EmoteWheelConfiguration _configuration;
        private readonly CameraLookLockModel _cameraLookLockModel;
        private readonly PlayerControlBlockModel _playerControlBlockModel;
        private readonly CursorModel _cursorModel;

        private Vector2 _pointer;

        public EmoteWheelSystem(
            IInputService inputService,
            IWindowsService windowsService,
            IGameFlowStateMachineService gameFlowStateMachineService,
            EmoteWheelModel wheelModel,
            EmoteCatalog catalog,
            EmoteWheelConfiguration configuration,
            CameraLookLockModel cameraLookLockModel,
            PlayerControlBlockModel playerControlBlockModel,
            CursorModel cursorModel) {
            _inputService = inputService;
            _windowsService = windowsService;
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _wheelModel = wheelModel;
            _catalog = catalog;
            _configuration = configuration;
            _cameraLookLockModel = cameraLookLockModel;
            _playerControlBlockModel = playerControlBlockModel;
            _cursorModel = cursorModel;
        }

        public void Initialize() {
            _inputService.Emote.Started += OnEmoteKeyPressed;
            _inputService.Emote.Canceled += OnEmoteKeyReleased;
            _inputService.Look.VectorChangedPerformed += OnLook;
            _gameFlowStateMachineService.StateExited += OnGameFlowStateExited;
        }

        public void Dispose() {
            _inputService.Emote.Started -= OnEmoteKeyPressed;
            _inputService.Emote.Canceled -= OnEmoteKeyReleased;
            _inputService.Look.VectorChangedPerformed -= OnLook;
            _gameFlowStateMachineService.StateExited -= OnGameFlowStateExited;
            CloseWheel();
        }

        private bool MayOpen =>
            _gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState)
            && _catalog.Emotes.Count > 0
            && _playerControlBlockModel.IsBlocked == false
            && _cursorModel.IsFreeCursorRequested == false;

        private void OnEmoteKeyPressed() {
            if (_wheelModel.IsOpen || MayOpen == false)
                return;

            _pointer = Vector2.zero;
            _wheelModel.Open();
            _cameraLookLockModel.Request(this);
            _windowsService.OpenWindow<EmoteWheelWindow>();
        }

        private void OnEmoteKeyReleased() {
            if (_wheelModel.IsOpen == false)
                return;

            int picked = _wheelModel.HighlightedIndex;
            CloseWheel();
            if (picked != EmoteWheelRule.NoSector)
                _wheelModel.Choose(picked);
        }

        private void OnLook(Vector2 delta) {
            if (_wheelModel.IsOpen == false)
                return;

            _pointer = EmoteWheelRule.MovePointer(_pointer, delta, _configuration.PointerRadius);
            _wheelModel.Highlight(EmoteWheelRule.PickSector(_pointer, _catalog.Emotes.Count, _configuration.DeadZoneRadius));
        }

        private void OnGameFlowStateExited(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                CloseWheel();
        }

        private void CloseWheel() {
            if (_wheelModel.IsOpen == false)
                return;

            _wheelModel.Close();
            _cameraLookLockModel.Release(this);
            if (_windowsService.GetWindow(typeof(EmoteWheelWindow)).WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<EmoteWheelWindow>();
        }
    }
}
