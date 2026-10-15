using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldObstacleScatterer {
        public void Scatter(FieldGenerationContext context, IReadOnlyList<Vector2Int> anchorCells, int targetCells, FieldReserveKind reserve);
    }
}
