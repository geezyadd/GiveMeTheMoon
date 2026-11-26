using Features.ExplosionModule.Configuration;
using Features.ExplosionModule.Wave;
using Mirror;
using UnityEngine;

namespace Features.ExplosionModule.Network {
    public sealed class ExplosionNetworkChannel : IExplosionNetworkChannel {
        private readonly ExplosionConfiguration _configuration;
        private readonly ExplosionWaves _waves;

        public ExplosionNetworkChannel(ExplosionConfiguration configuration, ExplosionWaves waves) {
            _configuration = configuration;
            _waves = waves;
        }

        public void RegisterForClient() =>
            NetworkClient.ReplaceHandler<ExplosionMessage>(OnExplosion);

        private void OnExplosion(ExplosionMessage message) {
            if (_configuration.TryGet(message.PresetIndex, out ExplosionPreset preset) == false) {
                Debug.LogError($"Explosion: no preset at index {message.PresetIndex}.");
                return;
            }

            _waves.Start(message.Center, preset, message.ShowsEffect);
        }
    }
}
