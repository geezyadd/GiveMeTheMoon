using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    public interface IFieldClearancePathFinder {
        // The shortest chain of side-connected cells from start to end the ship fits through (the passage clearance);
        // with avoidCorridors no corridor cell may be on it except the start and the end. Empty when there is none.
        public List<Vector2Int> Find(IMatrix<FieldCell> cells, Vector2Int start, Vector2Int end, bool avoidCorridors);
    }
}
