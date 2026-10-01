using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts.PlayerStats {
    public sealed class PlayerStatEntity : StatEntityMonoBase<PlayerStatType> {
        // StaticValueStat clamps AddValue to MaxValue. The cap is not a designer tunable.
        private const float STAT_MIN = 0f;
        private const float STAT_MAX = 100000f;

        private bool _cameraRelative = true;

        public bool CameraRelative => _cameraRelative;

        public float Read(PlayerStatType type) =>
            GetStat(type).FullValue;

        [Inject]
        private void InjectDependencies(PlayerStatsConfiguration configuration) =>
            ApplyDefaults(configuration);

        private void ApplyDefaults(PlayerStatsConfiguration configuration) {
            _cameraRelative = configuration.CameraRelative;
            foreach (PlayerStatType type in Enum.GetValues(typeof(PlayerStatType))) {
                if (type == PlayerStatType.None)
                    continue;

                IStat stat = GetStat(type);
                stat.MinValue = STAT_MIN;
                stat.MaxValue = STAT_MAX;
                stat.OverrideValue(DefaultOf(configuration, type));
            }
        }

        private static float DefaultOf(PlayerStatsConfiguration configuration, PlayerStatType type) {
            IReadOnlyList<PlayerStatsConfiguration.PlayerStatDefault> defaults = configuration.Defaults;
            for (int i = 0; i < defaults.Count; i++) {
                if (defaults[i].Type == type)
                    return defaults[i].Value;
            }

            throw new KeyNotFoundException($"Player stats configuration has no default for {type}.");
        }
    }
}
