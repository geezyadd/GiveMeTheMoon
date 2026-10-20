using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // What the obstacle field tests keep of one analysed field: the slalom way, the plane gap rule, the clearance of the
    // plane rocks and the numbers of the variant table. The rocks themselves are not kept: 2 400 fields would not fit.
    public sealed class FieldObstacleSummary {
        public int Seed { get; }
        public bool IsPathFound { get; }
        public float PathGapMeters { get; }
        public float PathToStraightRatio { get; }
        // The smallest gap in the flight plane between two debris rocks, a rock and a giant, or two giants.
        public float PlaneGapMeters { get; }
        public bool AreAllRocksInPlane { get; }
        public float RocksPerSquareKilometer { get; }
        public float LargestRockMeters { get; }
        public IReadOnlyList<int> SizeCounts { get; }
        public float DodgesPerMinute { get; }
        public int Giants { get; }

        public FieldObstacleSummary(int seed, FieldSlalomReport report, float planeGapMeters, bool areAllRocksInPlane, IReadOnlyList<int> sizeCounts, float dodgesPerMinute) {
            Seed = seed;
            IsPathFound = report.IsPathFound;
            PathGapMeters = report.MinGapMeters;
            PathToStraightRatio = report.PathToStraightRatio;
            PlaneGapMeters = planeGapMeters;
            AreAllRocksInPlane = areAllRocksInPlane;
            RocksPerSquareKilometer = report.PlaneRocksPerSquareKilometer();
            LargestRockMeters = report.LargestRockMeters();
            SizeCounts = sizeCounts;
            DodgesPerMinute = dodgesPerMinute;
            Giants = report.Plane.GiantDiametersMeters.Count;
        }
    }
}
