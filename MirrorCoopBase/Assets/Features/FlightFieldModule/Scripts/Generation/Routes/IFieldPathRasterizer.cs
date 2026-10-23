using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    public interface IFieldPathRasterizer {
        // Walks from the last cell of the path to the given cell by side moves and appends every cell on the way.
        public void Append(List<Vector2Int> path, Vector2Int cell);
        public Vector2Int ClampInside(Vector2 point, int inset, Vector2Int size);
    }
}
