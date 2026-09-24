using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Configurations;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(ShipAccumulativeStatsConfiguration) + "_Default",
        menuName = "Configurations/StatsModule/" + nameof(ShipAccumulativeStatsConfiguration))]
    public sealed class ShipAccumulativeStatsConfiguration : AccumulativeStatsConfigurationBase<ShipStatType> {
        private void OnEnable() {
            if (AccumulativeStats == null)
                AccumulativeStats = new System.Collections.Generic.List<ShipStatType> { ShipStatType.Armor };
        }
    }
}
