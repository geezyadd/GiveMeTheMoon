using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Configurations;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(ShipAccumulativeStatsConfiguration) + "_Default",
        menuName = "Configurations/StatsModule/" + nameof(ShipAccumulativeStatsConfiguration))]
    public sealed class ShipAccumulativeStatsConfiguration : AccumulativeStatsConfigurationBase<ShipStatType> {
        [SerializeField] private ShipStatDefault[] _defaults = Array.Empty<ShipStatDefault>();

        public IReadOnlyList<ShipStatDefault> Defaults => _defaults;

        private void OnEnable() {
            if (AccumulativeStats == null)
                AccumulativeStats = new List<ShipStatType> { ShipStatType.Armor };
        }

        private void OnValidate() {
            ShipStatType[] statTypes = (ShipStatType[])Enum.GetValues(typeof(ShipStatType));
            for (int i = 0; i < statTypes.Length; i++) {
                ShipStatType type = statTypes[i];
                int count = CountDefaults(type);
                bool isValid = type == ShipStatType.None ? count == 0 : count == 1;
                if (isValid == false)
                    UnityEngine.Debug.LogError($"{name}: {nameof(_defaults)} must list {type} exactly once (None never), found {count}.", this);
            }
        }

        private int CountDefaults(ShipStatType type) {
            int count = 0;
            for (int i = 0; i < _defaults.Length; i++) {
                if (_defaults[i].Type == type)
                    count++;
            }

            return count;
        }

        // The stat's cap is set before its value: an accumulative stat clamps the value to the cap.
        [Serializable]
        public sealed class ShipStatDefault {
            public ShipStatType Type;
            public float MaxValue;
            public float Value;
        }
    }
}
