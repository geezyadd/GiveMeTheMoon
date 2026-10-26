using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The rock cells of a generated field: its matrix. Touches no Unity object, so it runs on the generation thread.
    public sealed class MatrixFieldRockCells : IFieldRockCells {
        private readonly IMatrix<FieldCell> _cells;

        public MatrixFieldRockCells(IMatrix<FieldCell> cells) =>
            _cells = cells;

        public bool IsInField(Vector2Int cell) =>
            _cells.ValidateIndex(cell);

        public bool TryGetPlacementId(Vector2Int cell, out int placementId) {
            placementId = FieldCell.NO_PLACEMENT;
            if (!_cells.ValidateIndex(cell) || !_cells[cell].IsOccupied)
                return false;

            placementId = _cells[cell].PlacementId;
            return true;
        }

        public bool TryGetDebrisId(Vector2Int cell, out string debrisId) {
            debrisId = null;
            if (!_cells.ValidateIndex(cell) || !_cells[cell].HasDebris)
                return false;

            debrisId = _cells[cell].DebrisId;
            return true;
        }
    }
}
