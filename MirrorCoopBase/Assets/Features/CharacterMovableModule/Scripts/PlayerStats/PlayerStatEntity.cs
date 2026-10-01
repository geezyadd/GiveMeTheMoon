using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts.PlayerStats {
    // Runs after ZenAutoInjecter (order 0) on the spawned player, so the stat factory is injected before Awake.
    [DefaultExecutionOrder(1)]
    public sealed class PlayerStatEntity : StatEntityMonoBase<PlayerStatType> {
        // StaticValueStat clamps AddValue to MaxValue. The cap is not a designer tunable.
        private const float STAT_MIN = 0f;
        private const float STAT_MAX = 100000f;

        private static readonly PlayerStatType[] _statTypes = (PlayerStatType[])Enum.GetValues(typeof(PlayerStatType));

        private PlayerStatsConfiguration _configuration;
        private IStat[] _cachedStats;

        public bool CameraRelative => _configuration.CameraRelative;

        [Inject]
        private void InjectDependencies(PlayerStatsConfiguration configuration) =>
            _configuration = configuration;

        private void Awake() =>
            _cachedStats = CreateStatsWithDefaults();

        public float Read(PlayerStatType type) =>
            _cachedStats[(int)type].FullValue;

        private IStat[] CreateStatsWithDefaults() {
            // Enum.GetValues is sorted, so the last value is the highest index.
            IStat[] stats = new IStat[(int)_statTypes[_statTypes.Length - 1] + 1];
            IReadOnlyList<PlayerStatsConfiguration.PlayerStatDefault> defaults = _configuration.Defaults;
            for (int i = 0; i < defaults.Count; i++)
                ApplyDefault(stats, defaults[i]);

            for (int i = 0; i < _statTypes.Length; i++) {
                PlayerStatType type = _statTypes[i];
                if (type != PlayerStatType.None && stats[(int)type] == null)
                    throw new KeyNotFoundException($"Player stats configuration has no default for {type}.");
            }

            return stats;
        }

        private void ApplyDefault(IStat[] stats, PlayerStatsConfiguration.PlayerStatDefault statDefault) {
            PlayerStatType type = statDefault.Type;
            if (type == PlayerStatType.None || stats[(int)type] != null)
                throw new InvalidOperationException($"Player stats configuration has an invalid or duplicate default for {type}.");

            IStat stat = GetStat(type);
            stat.MinValue = STAT_MIN;
            stat.MaxValue = STAT_MAX;
            stat.OverrideValue(statDefault.Value);
            stats[(int)type] = stat;
        }
    }
}
