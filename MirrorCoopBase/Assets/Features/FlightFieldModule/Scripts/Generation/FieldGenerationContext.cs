using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation {
    // The working data of one generation attempt: the steps read the request and fill the rest in order.
    public sealed class FieldGenerationContext {
        private readonly List<FieldPlacement> _placements = new();
        private readonly List<FieldRoute> _routes = new();
        private readonly List<FieldTargetPlacement> _targets = new();

        private int _lastPlacementId = FieldCell.NO_PLACEMENT;

        public FieldRequest Request { get; }
        public int Seed { get; }
        public IFieldRandom Random { get; }
        public IReadOnlyList<FieldPlacement> Placements => _placements;
        public IReadOnlyList<FieldRoute> Routes => _routes;
        public IReadOnlyList<FieldTargetPlacement> Targets => _targets;
        public FieldDifficultyProfile Profile => Request.Profile;
        public float Difficulty => Request.Difficulty;
        public FieldMetrics Metrics { get; } = new();
        public FieldDensityMap Density { get; set; }
        public FieldFrame Frame { get; set; }
        public IMatrix<FieldCell> Cells { get; set; }
        public Vector2Int StationACell { get; set; }
        public Vector2Int StationBCell { get; set; }
        public Vector2Int CruiseStartCell { get; set; }
        public int NextPlacementId => _lastPlacementId + 1;

        public FieldGenerationContext(FieldRequest request, int seed, IFieldRandom random) {
            Request = request;
            Seed = seed;
            Random = random;
        }

        public void AddPlacement(FieldPlacement placement) {
            _placements.Add(placement);
            _lastPlacementId = placement.Id;
        }

        public void AddRoute(FieldRoute route) =>
            _routes.Add(route);

        public void AddTarget(FieldTargetPlacement target) =>
            _targets.Add(target);
    }
}
