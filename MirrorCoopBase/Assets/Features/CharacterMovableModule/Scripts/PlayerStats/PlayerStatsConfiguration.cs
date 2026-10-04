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

        private void OnValidate() {
            PlayerStatType[] statTypes = (PlayerStatType[])Enum.GetValues(typeof(PlayerStatType));
            for (int i = 0; i < statTypes.Length; i++) {
                PlayerStatType type = statTypes[i];
                int count = CountDefaults(type);
                bool isValid = type == PlayerStatType.None ? count == 0 : count == 1;
                if (isValid == false)
                    Debug.LogError($"{name}: {nameof(_defaults)} must list {type} exactly once (None never), found {count}.", this);
            }
        }

        private int CountDefaults(PlayerStatType type) {
            int count = 0;
            for (int i = 0; i < _defaults.Length; i++) {
                if (_defaults[i].Type == type)
                    count++;
            }

            return count;
        }

        [Serializable]
        public sealed class PlayerStatDefault {
            public PlayerStatType Type;
            public float Value;
        }
    }
}
