using System;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameCoreModule.Contracts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.UI;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // The local navigator's console window: it opens when the server seats this player, holds the player at the
    // console (no walking, no aim, a free cursor) and closes on Esc, on E or from the window's close button. Space
    // pings while it is open.
    public sealed class SonarWindowSystem : IInitializable, IDisposable, IGameplaySession {
        private readonly IInputService _inputService;
        private readonly IWindowsService _windowsService;
        private readonly SonarScopeModel _sonarScopeModel;
        private readonly SonarConsoleRequests _sonarConsoleRequests;
        private readonly PlayerControlBlockModel _playerControlBlockModel;
        private readonly CursorModel _cursorModel;

        private bool _isWindowShown;

        public SonarWindowSystem(
            IInputService inputService,
            IWindowsService windowsService,
            SonarScopeModel sonarScopeModel,
            SonarConsoleRequests sonarConsoleRequests,
            PlayerControlBlockModel playerControlBlockModel,
            CursorModel cursorModel) {
            _inputService = inputService;
            _windowsService = windowsService;
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleRequests = sonarConsoleRequests;
            _playerControlBlockModel = playerControlBlockModel;
            _cursorModel = cursorModel;
        }

        public void Initialize() {
            _sonarScopeModel.OnOpenChanged += OnScopeOpenChanged;
            _sonarConsoleRequests.OnCloseRequested += OnCloseRequested;
            _inputService.CloseWindow.Performed += OnCloseKeyPressed;
            _inputService.Grab.Performed += OnInteractKeyPressed;
            _inputService.Jump.Performed += OnPingKeyPressed;
        }

        public void Dispose() {
            _sonarScopeModel.OnOpenChanged -= OnScopeOpenChanged;
            _sonarConsoleRequests.OnCloseRequested -= OnCloseRequested;
            _inputService.CloseWindow.Performed -= OnCloseKeyPressed;
            _inputService.Grab.Performed -= OnInteractKeyPressed;
            _inputService.Jump.Performed -= OnPingKeyPressed;
            _sonarScopeModel.Close();
            HideWindow();
        }

        public void CleanupGameplay() =>
            _sonarScopeModel.Close();

        public void RestartGameplay() =>
            _sonarScopeModel.Close();

        private void ShowWindow() {
            if (_isWindowShown)
                return;

            _isWindowShown = true;
            _windowsService.OpenWindow<SonarWindow>();
            _playerControlBlockModel.Request(this);
            _cursorModel.RequestFreeCursor();
        }

        private void HideWindow() {
            if (_isWindowShown == false)
                return;

            _isWindowShown = false;
            IWindow window = _windowsService.GetWindow(typeof(SonarWindow));
            if (window.WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<SonarWindow>();

            _playerControlBlockModel.Release(this);
            _cursorModel.ReleaseFreeCursor();
        }

        private void OnScopeOpenChanged() {
            if (_sonarScopeModel.IsOpen)
                ShowWindow();
            else
                HideWindow();
        }

        private void OnCloseRequested() =>
            _sonarScopeModel.Close();

        private void OnCloseKeyPressed() =>
            _sonarScopeModel.Close();

        // On the host the server seats the player within the same E press that asked for the console: that press
        // must not close it again.
        private void OnInteractKeyPressed() {
            if (_sonarScopeModel.IsOpen && _sonarScopeModel.OpenedFrame != Time.frameCount)
                _sonarScopeModel.Close();
        }

        private void OnPingKeyPressed() {
            if (_sonarScopeModel.IsOpen)
                _sonarConsoleRequests.RequestPing();
        }
    }
}
