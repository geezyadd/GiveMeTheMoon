using System;
using Features.CharacterMovableModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Models;
using Zenject;

namespace Features.CutsceneModule.Scripts.Client {
    // Every peer: while this peer plays a cutscene its player neither walks, aims, uses, fires nor steers.
    public sealed class CutscenePlayerLockSystem : IInitializable, IDisposable {
        private readonly CutsceneLocalModel _cutsceneLocalModel;
        private readonly PlayerControlBlockModel _playerControlBlockModel;

        public CutscenePlayerLockSystem(CutsceneLocalModel cutsceneLocalModel, PlayerControlBlockModel playerControlBlockModel) {
            _cutsceneLocalModel = cutsceneLocalModel;
            _playerControlBlockModel = playerControlBlockModel;
        }

        public void Initialize() =>
            _cutsceneLocalModel.OnPlayingChanged += OnPlayingChanged;

        public void Dispose() {
            _cutsceneLocalModel.OnPlayingChanged -= OnPlayingChanged;
            _playerControlBlockModel.Release(this);
        }

        private void OnPlayingChanged() {
            if (_cutsceneLocalModel.IsPlaying)
                _playerControlBlockModel.Request(this);
            else
                _playerControlBlockModel.Release(this);
        }
    }
}
