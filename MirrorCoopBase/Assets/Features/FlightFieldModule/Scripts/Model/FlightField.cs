using System.Collections.Generic;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // A generated field: its frame, the placements standing in it and the routes through it.
    public sealed class FlightField {
        public FieldFrame Frame { get; }
        // The seed the field was built with: the request seed, or a derived one when the request seed gave no passage.
        public int Seed { get; }
        // The working grid of the generator, kept for the debug map and tests; the placements and debris cells are
        // the content.
        public IMatrix<FieldCell> Cells { get; }
        public IReadOnlyList<FieldPlacement> Placements { get; }
        public IReadOnlyList<FieldRoute> Routes { get; }
        public Vector2Int StationACell { get; }
        public Vector2Int StationBCell { get; }
        public Vector2Int CruiseStartCell { get; }
        // True when no seed gave a passage and the route corridors were cleared by force.
        public bool IsPassageForced { get; }
        public string ProfileId { get; }
        public float Difficulty { get; }
        public FieldDensityMap Density { get; }
        public FieldMetrics Metrics { get; }

        public FlightField(FieldFrame frame, int seed, IMatrix<FieldCell> cells, IReadOnlyList<FieldPlacement> placements, IReadOnlyList<FieldRoute> routes,
                           Vector2Int stationACell, Vector2Int stationBCell, Vector2Int cruiseStartCell, bool isPassageForced, string profileId, float difficulty,
                           FieldDensityMap density, FieldMetrics metrics) {
            Frame = frame;
            Seed = seed;
            Cells = cells;
            Placements = placements;
            Routes = routes;
            StationACell = stationACell;
            StationBCell = stationBCell;
            CruiseStartCell = cruiseStartCell;
            IsPassageForced = isPassageForced;
            ProfileId = profileId;
            Difficulty = difficulty;
            Density = density;
            Metrics = metrics;
        }
    }
}
