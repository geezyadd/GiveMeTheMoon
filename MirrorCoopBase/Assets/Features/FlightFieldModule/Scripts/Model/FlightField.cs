using System.Collections.Generic;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // A generated field: its frame, the placements standing in it and the routes through it.
    public sealed class FlightField {
        public FieldFrame Frame { get; }
        // The seed the field was built with: the request seed.
        public int Seed { get; }
        // The working grid of the generator, kept for the debug map and tests; the placements and debris cells are
        // the content.
        public IMatrix<FieldCell> Cells { get; }
        public IReadOnlyList<FieldPlacement> Placements { get; }
        public IReadOnlyList<FieldRoute> Routes { get; }
        // The middles of the points of interest in field space (FieldFrame.FieldToWorld), on the flight plane.
        public IReadOnlyList<Vector3> PointsOfInterest { get; }
        public Vector2Int StationACell { get; }
        public Vector2Int StationBCell { get; }
        public Vector2Int CruiseStartCell { get; }
        public string ProfileId { get; }
        public float Difficulty { get; }
        public FieldDensityMap Density { get; }
        public FieldMetrics Metrics { get; }
        public int LoopIndex { get; }

        public FlightField(FieldFrame frame, int seed, IMatrix<FieldCell> cells, IReadOnlyList<FieldPlacement> placements, IReadOnlyList<FieldRoute> routes,
                           Vector2Int stationACell, Vector2Int stationBCell, Vector2Int cruiseStartCell, string profileId, float difficulty,
                           FieldDensityMap density, FieldMetrics metrics, int loopIndex, IReadOnlyList<Vector3> pointsOfInterest) {
            Frame = frame;
            Seed = seed;
            Cells = cells;
            Placements = placements;
            Routes = routes;
            StationACell = stationACell;
            StationBCell = stationBCell;
            CruiseStartCell = cruiseStartCell;
            ProfileId = profileId;
            Difficulty = difficulty;
            Density = density;
            Metrics = metrics;
            LoopIndex = loopIndex;
            PointsOfInterest = pointsOfInterest;
        }
    }
}
