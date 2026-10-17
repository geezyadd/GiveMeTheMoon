using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The slalom numbers of a field: the way a ship of the configured width (plus the gap margin) flies from the cruise
    // start to B between the debris rocks and the walls, how long it is against the straight line, the smallest gap
    // along it and how dense the rocks of the main channels are.
    public sealed class FieldSlalomReport {
        private const float METERS_PER_KILOMETER = 1000f;

        public bool IsPathFound { get; }
        // Field plane points (x across, y along), from the cruise start to B; empty when no way was found.
        public IReadOnlyList<Vector2> Path { get; }
        public IReadOnlyList<FieldSlalomRock> Rocks { get; }
        public float PathLengthMeters { get; }
        public float StraightLengthMeters { get; }
        // Twice the smallest distance from the path to a rock or a wall: the narrowest gap the ship goes through.
        public float MinGapMeters { get; }
        public int ChannelRocks { get; }
        public float ChannelLengthMeters { get; }
        public float PathToStraightRatio => StraightLengthMeters > 0f ? PathLengthMeters / StraightLengthMeters : 0f;
        public float RocksPerKilometer => ChannelLengthMeters > 0f ? ChannelRocks / (ChannelLengthMeters / METERS_PER_KILOMETER) : 0f;

        public FieldSlalomReport(bool isPathFound, IReadOnlyList<Vector2> path, IReadOnlyList<FieldSlalomRock> rocks, float pathLengthMeters, float straightLengthMeters,
                                 float minGapMeters, int channelRocks, float channelLengthMeters) {
            IsPathFound = isPathFound;
            Path = path;
            Rocks = rocks;
            PathLengthMeters = pathLengthMeters;
            StraightLengthMeters = straightLengthMeters;
            MinGapMeters = minGapMeters;
            ChannelRocks = channelRocks;
            ChannelLengthMeters = channelLengthMeters;
        }
    }
}
