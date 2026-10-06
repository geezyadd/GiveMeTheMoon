using System;
using System.Collections.Generic;
using Zenject;

namespace Features.ShipModule.Scripts {
    // The one check of the ship stat defaults: every stat exactly once, None never. Runs once at startup.
    public sealed class ShipStatsConfigurationValidator : IInitializable {
        private readonly ShipAccumulativeStatsConfiguration _shipAccumulativeStatsConfiguration;

        public ShipStatsConfigurationValidator(ShipAccumulativeStatsConfiguration shipAccumulativeStatsConfiguration) =>
            _shipAccumulativeStatsConfiguration = shipAccumulativeStatsConfiguration;

        public void Initialize() {
            ShipStatType[] statTypes = (ShipStatType[])Enum.GetValues(typeof(ShipStatType));
            for (int i = 0; i < statTypes.Length; i++)
                ValidateStat(statTypes[i]);
        }

        private void ValidateStat(ShipStatType type) {
            int count = CountDefaults(type);
            bool isValid = type == ShipStatType.None ? count == 0 : count == 1;
            if (isValid == false)
                throw new InvalidOperationException(
                    $"{_shipAccumulativeStatsConfiguration.name}: {nameof(ShipAccumulativeStatsConfiguration.Defaults)} must list {type} exactly once (None never), found {count}.");
        }

        private int CountDefaults(ShipStatType type) {
            IReadOnlyList<ShipAccumulativeStatsConfiguration.ShipStatDefault> defaults = _shipAccumulativeStatsConfiguration.Defaults;
            int count = 0;
            for (int i = 0; i < defaults.Count; i++) {
                if (defaults[i].Type == type)
                    count++;
            }

            return count;
        }
    }
}
