using System;
using System.Collections.Generic;
using Features.AudioModule;
using Features.AudioModule.Ambience;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using UnityEngine;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // Plays the session music from entering the session (it starts in the lobby) and stops it on leaving to the menu.
    // Local on each peer: every player hears it from their own game flow. It goes down to the ducked volume while a
    // MusicDuckSources source (a dancing player) is within the duck radius of the camera, and comes back after.
    public sealed class AmbienceSystem : IInitializable, ITickable, IDisposable {
        private readonly IGameFlowStateMachineService _gameFlow;
        private readonly MusicDuckSources _duckSources;
        private readonly AmbienceConfiguration _configuration;
        private readonly SoundInstance _music;
        private float _volume = 1f;

        public AmbienceSystem(IGameFlowStateMachineService gameFlow, IAudioService audioService,
                              AmbienceConfiguration configuration, MusicDuckSources duckSources) {
            _gameFlow = gameFlow;
            _configuration = configuration;
            _duckSources = duckSources;
            _music = audioService.CreateInstance(configuration.SessionMusic);
        }

        public void Initialize() {
            _gameFlow.StateEntered += OnStateEntered;
            _gameFlow.StateExited += OnStateExited;
        }

        public void Dispose() {
            _gameFlow.StateEntered -= OnStateEntered;
            _gameFlow.StateExited -= OnStateExited;
            _music.Dispose();
        }

        public void Tick() {
            float target = IsDuckSourceNear() ? _configuration.DuckedVolume : 1f;
            if (Mathf.Approximately(_volume, target))
                return;

            _volume = Mathf.MoveTowards(_volume, target, _configuration.DuckSpeed * Time.unscaledDeltaTime);
            _music.SetVolume(_volume);
        }

        private bool IsDuckSourceNear() {
            IReadOnlyList<Transform> sources = _duckSources.Sources;
            if (sources.Count == 0)
                return false;

            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Vector3 listener = camera.transform.position;
            float radiusSquared = _configuration.DuckRadius * _configuration.DuckRadius;
            for (int i = 0; i < sources.Count; i++) {
                if (sources[i] != null && (sources[i].position - listener).sqrMagnitude <= radiusSquared)
                    return true;
            }

            return false;
        }

        private void OnStateEntered(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _music.Start();
        }

        private void OnStateExited(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _music.Stop();
        }
    }
}
