using System;
using Features.CutsceneModule.Scripts.Models;
using Features.LobbyModule.Scripts;
using Features.MvpModule;
using Zenject;

namespace Features.CutsceneModule.Scripts.Client {
    // Every peer: the game HUD is hidden while this peer plays a cutscene and shown again after, if it was shown.
    public sealed class CutsceneHudSystem : IInitializable, IDisposable {
        private readonly CutsceneLocalModel _cutsceneLocalModel;
        private readonly IWindowsService _windowsService;

        private bool _isHudHidden;

        public CutsceneHudSystem(CutsceneLocalModel cutsceneLocalModel, IWindowsService windowsService) {
            _cutsceneLocalModel = cutsceneLocalModel;
            _windowsService = windowsService;
        }

        public void Initialize() =>
            _cutsceneLocalModel.OnPlayingChanged += OnPlayingChanged;

        public void Dispose() =>
            _cutsceneLocalModel.OnPlayingChanged -= OnPlayingChanged;

        private void OnPlayingChanged() {
            if (_cutsceneLocalModel.IsPlaying)
                HideHud();
            else
                ShowHud();
        }

        private void HideHud() {
            if (_windowsService.GetWindow(typeof(GameHudWindow)).WindowStatus != WindowStatus.Showed)
                return;

            _windowsService.HideWindow<GameHudWindow>();
            _isHudHidden = true;
        }

        // The session may have closed the HUD meanwhile (back to the menu): only a still hidden HUD comes back.
        private void ShowHud() {
            if (!_isHudHidden)
                return;

            _isHudHidden = false;
            if (_windowsService.GetWindow(typeof(GameHudWindow)).WindowStatus == WindowStatus.Hidden)
                _windowsService.ShowWindow<GameHudWindow>();
        }
    }
}
