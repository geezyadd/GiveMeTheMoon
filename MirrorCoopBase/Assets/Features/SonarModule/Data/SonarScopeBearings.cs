using UnityEngine;

namespace Features.SonarModule.Data {
    // Where the ship heads and where the next station lies, as the scope shows them without a ping.
    public readonly struct SonarScopeBearings {
        public float HeadingDegrees { get; }
        public Vector2 NorthScopePosition { get; }
        public bool HasStation { get; }
        public Vector2 StationScopePosition { get; }
        public float StationDistanceMeters { get; }

        public SonarScopeBearings(float headingDegrees, Vector2 northScopePosition, bool hasStation, Vector2 stationScopePosition, float stationDistanceMeters) {
            HeadingDegrees = headingDegrees;
            NorthScopePosition = northScopePosition;
            HasStation = hasStation;
            StationScopePosition = stationScopePosition;
            StationDistanceMeters = stationDistanceMeters;
        }
    }
}
