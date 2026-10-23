using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    // Looks for a way from the start to the end of a route over cells the ship fits through: the cell and every cell
    // within the clearance around it (diagonals included) are inside the field and have no structure.
    public sealed class FieldPassageChecker : IFieldPassageChecker {
        private readonly IFieldClearancePathFinder _fieldClearancePathFinder;

        public FieldPassageChecker(IFieldClearancePathFinder fieldClearancePathFinder) =>
            _fieldClearancePathFinder = fieldClearancePathFinder;

        public bool HasPassage(IMatrix<FieldCell> cells, FieldRoute route) =>
            _fieldClearancePathFinder.Find(cells, route.Start, route.End, false).Count > 0;
    }
}
