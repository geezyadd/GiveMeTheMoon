using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    public interface IFieldBranchPlanner {
        // A centerline that leaves the main centerline at one index, swings to the side (+1 or -1) and comes back at
        // a later index.
        public List<Vector2Int> PlanAlternative(FieldGenerationContext context, IReadOnlyList<Vector2Int> main, Vector2Int span, int side, float offsetCells, int inset);
        // A centerline that leaves the main centerline at the index forward and to the side and ends after the length.
        public List<Vector2Int> PlanDeadEnd(FieldGenerationContext context, IReadOnlyList<Vector2Int> main, int fromIndex, int side, float lengthCells, int inset);
    }
}
