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

        [Serializable]
        public sealed class ShipStatDefault {
            public ShipStatType Type;
            public float MaxValue;
            public float Value;
        }
    }
}
