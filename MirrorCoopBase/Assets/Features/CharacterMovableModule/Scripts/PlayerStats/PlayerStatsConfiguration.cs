using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Configurations;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.PlayerStats {
    [CreateAssetMenu(
        fileName = nameof(PlayerStatsConfiguration) + "_Default",
        menuName = "Configurations/CharacterMovableModule/" + nameof(PlayerStatsConfiguration))]
    public sealed class PlayerStatsConfiguration : AccumulativeStatsConfigurationBase<PlayerStatType> {
        [SerializeField] private PlayerStatDefault[] _defaults = Array.Empty<PlayerStatDefault>();
        [SerializeField] private bool _cameraRelative = true;

        public IReadOnlyList<PlayerStatDefault> Defaults => _defaults;
        public bool CameraRelative => _cameraRelative;

        [Serializable]
        public sealed class PlayerStatDefault {
            public PlayerStatType Type;
            public float Value;
        }
    }
}
