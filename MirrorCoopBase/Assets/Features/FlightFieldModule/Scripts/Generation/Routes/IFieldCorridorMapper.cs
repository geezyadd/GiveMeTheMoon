using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    public interface IFieldCorridorMapper {
        // Cells within the half width (diagonals included) of any centerline cell, in row order.
        public List<Vector2Int> Map(Vector2Int size, IReadOnlyList<Vector2Int> centerline, int halfWidthCells);
        // The same with one half width per centerline cell.
        public List<Vector2Int> Map(Vector2Int size, IReadOnlyList<Vector2Int> centerline, IReadOnlyList<int> halfWidths);
    }
}
