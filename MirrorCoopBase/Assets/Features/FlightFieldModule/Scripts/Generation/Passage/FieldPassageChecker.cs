using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using Features.MatrixModule.Scripts.MatrixFactory;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    // Looks for a way from the start to the end of a route over cells the ship fits through: the cell and every cell
    // within the clearance around it (diagonals included) are inside the field and have no structure.
    public sealed class FieldPassageChecker : IFieldPassageChecker {
        private readonly IMatrixFactory _matrixFactory;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldPassageChecker(IMatrixFactory matrixFactory, FlightFieldConfiguration flightFieldConfiguration) {
            _matrixFactory = matrixFactory;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public bool HasPassage(IMatrix<FieldCell> cells, FieldRoute route) =>
            CreatePassableMatrix(cells).PathFinding(route.Start, route.End, isDiagonalMove: true, predicate: IsPassable).Count > 0;

        private IMatrix<bool> CreatePassableMatrix(IMatrix<FieldCell> cells) {
            int side = cells.GetRowCount();
            int clearance = _flightFieldConfiguration.PassageClearanceCells;
            IMatrix<bool> passable = _matrixFactory.Create(side, side, false);
            for (int y = 0; y < side; y++) {
                for (int x = 0; x < side; x++)
                    passable.SetValue(x, y, HasClearance(cells, x, y, clearance));
            }

            return passable;
        }

        private static bool HasClearance(IMatrix<FieldCell> cells, int x, int y, int clearance) {
            for (int nearY = y - clearance; nearY <= y + clearance; nearY++) {
                if (!IsRowClear(cells, x, nearY, clearance))
                    return false;
            }

            return true;
        }

        private static bool IsRowClear(IMatrix<FieldCell> cells, int x, int y, int clearance) {
            for (int nearX = x - clearance; nearX <= x + clearance; nearX++) {
                if (IsBlocked(cells, nearX, y))
                    return false;
            }

            return true;
        }

        // Outside the field counts as blocked: the ship must not leave it.
        private static bool IsBlocked(IMatrix<FieldCell> cells, int x, int y) =>
            !cells.ValidateIndex(x, y) || cells.GetValue(x, y).IsOccupied;

        private static bool IsPassable(bool cell) =>
            cell;
    }
}
