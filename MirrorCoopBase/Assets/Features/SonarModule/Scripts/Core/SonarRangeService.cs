using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // The sonar range is the ship's synced SonarRange stat (radar modules raise it), never below the configured floor.
    public sealed class SonarRangeService : ISonarRangeService {
        private readonly IReadOnlyShipStatsModel _shipStatsModel;
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarRangeService(IReadOnlyShipStatsModel shipStatsModel, SonarConfiguration sonarConfiguration) {
            _shipStatsModel = shipStatsModel;
            _sonarConfiguration = sonarConfiguration;
        }

        public float GetRangeMeters() =>
            Mathf.Max(_shipStatsModel.SonarRange, _sonarConfiguration.MinRangeMeters);
    }
}
