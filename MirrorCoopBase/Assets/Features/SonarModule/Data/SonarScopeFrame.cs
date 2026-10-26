using UnityEngine;

namespace Features.SonarModule.Data {
    // The scope's numbers this frame: range, the ping wave (below 0: no wave), the cooldown (1 = ready), whether the
    // radar works, the crew's marks against the limit, the ship's heading with where north lies on the scope, and
    // the next station's place on the scope (on its edge when out of range) and distance.
    public readonly struct SonarScopeFrame {
        public float RangeMeters { get; }
        public float WaveShare { get; }
        public float CooldownShare { get; }
        public bool IsWorking { get; }
        public int MarkCount { get; }
        public int MarkLimit { get; }
        public float HeadingDegrees { get; }
        public Vector2 NorthScopePosition { get; }
        public bool HasStation { get; }
        public Vector2 StationScopePosition { get; }
        public float StationDistanceMeters { get; }

        public SonarScopeFrame(float rangeMeters, float waveShare, float cooldownShare, bool isWorking, int markCount, int markLimit, SonarScopeBearings bearings) {
            RangeMeters = rangeMeters;
            WaveShare = waveShare;
            CooldownShare = cooldownShare;
            IsWorking = isWorking;
            MarkCount = markCount;
            MarkLimit = markLimit;
            HeadingDegrees = bearings.HeadingDegrees;
            NorthScopePosition = bearings.NorthScopePosition;
            HasStation = bearings.HasStation;
            StationScopePosition = bearings.StationScopePosition;
            StationDistanceMeters = bearings.StationDistanceMeters;
        }
    }
}
