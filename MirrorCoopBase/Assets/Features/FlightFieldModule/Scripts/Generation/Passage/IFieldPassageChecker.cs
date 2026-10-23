using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    public interface IFieldPassageChecker {
        public bool HasPassage(IMatrix<FieldCell> cells, FieldRoute route);
    }
}
