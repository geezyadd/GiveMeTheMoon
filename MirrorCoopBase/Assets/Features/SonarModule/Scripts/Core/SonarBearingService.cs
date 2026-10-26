using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Sources;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // What the scope shows without a ping: the ship's heading (0 = north, the world's +Z), where north lies on the
    // scope's edge, and the next station on the scope (on its edge when beyond range) with its distance.
    public sealed class SonarBearingService : ISonarBearingService {
        private const float FULL_TURN_DEGREES = 360f;

        private readonly ISonarScopeLayout _sonarScopeLayout;
        private readonly IStationSonarContactSource _stationSonarContactSource;

        public SonarBearingService(ISonarScopeLayout sonarScopeLayout, IStationSonarContactSource stationSonarContactSource) {
            _sonarScopeLayout = sonarScopeLayout;
            _stationSonarContactSource = stationSonarContactSource;
        }

        public SonarScopeBearings GetBearings(Vector3 shipPosition, float shipYawDegrees, float rangeMeters) {
            float heading = Mathf.Repeat(shipYawDegrees, FULL_TURN_DEGREES);
            _sonarScopeLayout.TryPlace(shipPosition, shipYawDegrees, shipPosition + Vector3.forward * rangeMeters, rangeMeters, true, out Vector2 north);
            if (_stationSonarContactSource.TryGetStation(out SonarContact station) == false)
                return new SonarScopeBearings(heading, north, false, Vector2.zero, 0f);

            _sonarScopeLayout.TryPlace(shipPosition, shipYawDegrees, station.Position, rangeMeters, true, out Vector2 stationOnScope);
            float distance = _sonarScopeLayout.GetDistanceShare(shipPosition, station.Position, rangeMeters) * rangeMeters;
            return new SonarScopeBearings(heading, north, true, stationOnScope, distance);
        }
    }
}
