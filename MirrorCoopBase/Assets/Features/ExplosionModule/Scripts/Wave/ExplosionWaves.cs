using System.Collections.Generic;
using Features.ExplosionModule.Configuration;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ExplosionModule.Wave {
    // The explosion waves running on this peer: starts a wave (and, for an explosion that brings no look of its own,
    // its effect and shake), steps every wave each physics step and drops the ones that reached their radius.
    public sealed class ExplosionWaves : IFixedTickable {
        private const int OVERLAP_BUFFER_SIZE = 256;

        private readonly ExplosionConfiguration _configuration;
        private readonly ExplosionReceiverRegistry _registry;
        private readonly IExplosionEffects _effects;
        private readonly List<ExplosionWave> _waves = new();
        private readonly Collider[] _overlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];

        public ExplosionWaves(ExplosionConfiguration configuration, ExplosionReceiverRegistry registry, IExplosionEffects effects) {
            _configuration = configuration;
            _registry = registry;
            _effects = effects;
        }

        public void Start(Vector3 center, ExplosionPreset preset, bool showsEffect) {
            _waves.Add(new ExplosionWave(center, preset));
            if (showsEffect)
                _effects.Play(center, preset);
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
