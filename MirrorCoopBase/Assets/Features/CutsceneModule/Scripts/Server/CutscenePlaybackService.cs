using System;
using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using Features.CutsceneModule.Scripts.Core;
using Features.CutsceneModule.Scripts.Network;
using Features.CutsceneModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts.Contracts;
using Mirror;

namespace Features.CutsceneModule.Scripts.Server {
    // Server: a cutscene is one synced playback; a new one takes the next sequence and starts a moment ahead, so every
    // client has it before its first frame.
    public sealed class CutscenePlaybackService : ICutsceneDirector {
        private readonly IReadOnlyComponentRegistry<CutsceneSync> _cutsceneSyncRegistry;
        private readonly IReadOnlyCutsceneModel _cutsceneModel;
        private readonly ICutscenePlaybackRule _cutscenePlaybackRule;
        private readonly CutsceneConfiguration _cutsceneConfiguration;

        public bool IsPlaying => _cutsceneModel.IsAvailable && _cutscenePlaybackRule.IsRunning(_cutsceneModel.Playback, NetworkTime.time);

        public CutscenePlaybackService(IReadOnlyComponentRegistry<CutsceneSync> cutsceneSyncRegistry, IReadOnlyCutsceneModel cutsceneModel,
                                       ICutscenePlaybackRule cutscenePlaybackRule, CutsceneConfiguration cutsceneConfiguration) {
            _cutsceneSyncRegistry = cutsceneSyncRegistry;
            _cutsceneModel = cutsceneModel;
            _cutscenePlaybackRule = cutscenePlaybackRule;
            _cutsceneConfiguration = cutsceneConfiguration;
        }

        public void ServerPlay(IReadOnlyList<CutsceneShot> shots) {
            CutsceneShot[] copy = new CutsceneShot[shots.Count];
            float shotsSeconds = 0f;
            for (int i = 0; i < shots.Count; i++) {
                copy[i] = shots[i];
                shotsSeconds += shots[i].Seconds;
            }

            GetSync().ServerSetPlayback(new CutscenePlayback {
                Sequence = _cutsceneModel.Playback.Sequence + 1,
                StartTime = NetworkTime.time + _cutsceneConfiguration.StartLeadSeconds,
                ShotsSeconds = shotsSeconds,
                IsSkipped = false,
                Shots = copy,
            });
        }

        public void ServerSkip() {
            if (!IsPlaying)
                return;

            CutscenePlayback skipped = _cutsceneModel.Playback;
            skipped.IsSkipped = true;
            GetSync().ServerSetPlayback(skipped);
        }

        // A cutscene plays only in flight, while the ship and its identity stand.
        private CutsceneSync GetSync() {
            if (_cutsceneSyncRegistry.Items.Count == 0)
                throw new InvalidOperationException("No cutscene sync on the server: a cutscene needs the run's ship.");

            return _cutsceneSyncRegistry.Items[0];
        }
    }
}
