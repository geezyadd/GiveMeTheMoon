using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    public interface IFieldCenterlinePlanner {
        // Ordered connected cells from the cruise start to station B.
        public List<Vector2Int> Plan(FieldGenerationContext context, FieldCorridorShape shape);
    }
}
