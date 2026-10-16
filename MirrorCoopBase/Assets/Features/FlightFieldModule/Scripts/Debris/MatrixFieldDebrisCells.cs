using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // The debris cells as the generator sees them: its matrix. Touches no Unity object, so it runs on the generation
    // thread.
    public sealed class MatrixFieldDebrisCells : IFieldDebrisCells {
        private readonly IMatrix<FieldCell> _cells;
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public MatrixFieldDebrisCells(IMatrix<FieldCell> cells, FieldStructureCatalog fieldStructureCatalog) {
            _cells = cells;
            _fieldStructureCatalog = fieldStructureCatalog;
        }

        public bool TryGetDebris(Vector2Int cell, out float density, out FieldDebrisRecipe recipe) {
            density = 0f;
            recipe = null;
            if (!_cells.ValidateIndex(cell) || !_cells[cell].HasDebris)
                return false;

            density = _cells[cell].DebrisDensity;
            recipe = _fieldStructureCatalog.GetRequiredDebris(_cells[cell].DebrisId).Recipe;
            return true;
        }

        public bool IsBlocked(Vector2Int cell) =>
            _cells.ValidateIndex(cell) && _cells[cell].IsOccupied;

        public bool IsEmpty(Vector2Int cell) =>
            !_cells.ValidateIndex(cell) || !_cells[cell].IsOccupied && !_cells[cell].HasDebris;
    }
}
