using Features.ExplosionModule.Configuration;
using Features.ExplosionModule.Network;
using Features.ExplosionModule.Wave;
using Mirror;
using UnityEngine;

namespace Features.ExplosionModule {
    // The server sends the explosion to every client (the host's own one too); each peer's channel starts the wave.
    // A server without a local client runs the wave itself, since loose items are thrown on the server.
    public sealed class ExplosionService : IExplosionService {
        private readonly ExplosionConfiguration _configuration;
        private readonly ExplosionWaves _waves;

        public ExplosionService(ExplosionConfiguration configuration, ExplosionWaves waves) {
            _configuration = configuration;
            _waves = waves;
        }

        public void ServerExplode(Vector3 center, ExplosionPreset preset, bool showsEffect = true) {
            if (NetworkServer.active == false)
                return;

            int index = _configuration.IndexOf(preset);
            if (index < 0 || index > byte.MaxValue) {
                Debug.LogError($"Explosion: preset {(preset != null ? preset.name : "null")} is not in the configuration.");
                return;
            }

            NetworkServer.SendToAll(new ExplosionMessage { Center = center, PresetIndex = (byte)index, ShowsEffect = showsEffect });
            if (NetworkClient.active == false)
                _waves.Start(center, preset, false);
        }
    }
}
