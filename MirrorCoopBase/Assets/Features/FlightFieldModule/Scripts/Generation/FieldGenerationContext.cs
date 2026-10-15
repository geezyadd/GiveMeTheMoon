using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation {
    // The working data of one generation attempt: the steps read the request and fill the rest in order.
    public sealed class FieldGenerationContext {
        private int _lastPlacementId = FieldCell.NO_PLACEMENT;

        public FieldRequest Request { get; }
        public int Seed { get; }
        public IFieldRandom Random { get; }
        public List<FieldPlacement> Placements { get; } = new();
        public List<FieldRoute> Routes { get; } = new();
        public FieldFrame Frame { get; set; }
        public IMatrix<FieldCell> Cells { get; set; }
        public Vector2Int StationACell { get; set; }
        public Vector2Int StationBCell { get; set; }
        public Vector2Int CruiseStartCell { get; set; }
        public bool HasPassage { get; set; }
        public int NextPlacementId => _lastPlacementId + 1;

        public FieldGenerationContext(FieldRequest request, int seed, IFieldRandom random) {
            Request = request;
            Seed = seed;
            Random = random;
        }

        public void AddPlacement(FieldPlacement placement) {
            Placements.Add(placement);
            _lastPlacementId = placement.Id;
        }
    }
}
