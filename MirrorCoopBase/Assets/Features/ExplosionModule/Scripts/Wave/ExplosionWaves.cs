using System.Collections.Generic;
using Features.EffectPoolModule.Scripts;
using Features.ExplosionModule.Configuration;
using Features.ExplosionModule.Visual;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ExplosionModule.Wave {
    // The explosion waves running on this peer: starts a wave and its sphere, steps every wave each physics step and
    // drops the ones that reached their radius.
    public sealed class ExplosionWaves : IFixedTickable {
        private const int OVERLAP_BUFFER_SIZE = 256;

        private readonly ExplosionConfiguration _configuration;
        private readonly ExplosionReceiverRegistry _registry;
        private readonly SelfDisablingEffectPool<ExplosionSphere> _spheres = new();
        private readonly List<ExplosionWave> _waves = new();
        private readonly Collider[] _overlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];

        public ExplosionWaves(ExplosionConfiguration configuration, ExplosionReceiverRegistry registry) {
            _configuration = configuration;
            _registry = registry;
        }

        public void Start(Vector3 center, ExplosionPreset preset) {
            _waves.Add(new ExplosionWave(center, preset));
            if (_configuration.SpherePrefab != null)
                _spheres.TakeFree(_configuration.SpherePrefab, null).Play(center, preset);
        }

        public void FixedTick() {
            if (_waves.Count == 0)
                return;

            var context = new ExplosionStepContext(NetworkServer.active, _configuration.AffectedLayers, _overlapBuffer);
            for (int i = _waves.Count - 1; i >= 0; i--) {
                ExplosionWave wave = _waves[i];
                wave.Step(Time.fixedDeltaTime, _registry, context);
                if (wave.IsOver)
                    _waves.RemoveAt(i);
            }
        }
    }
}
