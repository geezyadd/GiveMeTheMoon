using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    public interface IFieldCenterlinePlanner {
        // Side-connected cells from start to end; the curve between them stays at least the inset away from the border.
        public List<Vector2Int> Plan(FieldGenerationContext context, Vector2Int start, Vector2Int end, FieldCorridorShape shape, int inset);
    }
}
