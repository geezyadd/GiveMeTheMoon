using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Cells of the field plane as the slalom rocks see them: plane x across, y along; the cell is the matrix one
    // (x along, y across).
    public static class FieldPlaneCells {
        public static Vector2Int ToCell(Vector2 point, float cellSize) =>
            new(Mathf.FloorToInt(point.y / cellSize), Mathf.FloorToInt(point.x / cellSize));

        public static float DistanceToCell(Vector2 point, Vector2Int cell, float cellSize) {
            Vector2 min = new(cell.y * cellSize, cell.x * cellSize);
            Vector2 nearest = new(Mathf.Clamp(point.x, min.x, min.x + cellSize), Mathf.Clamp(point.y, min.y, min.y + cellSize));
            return Vector2.Distance(point, nearest);
        }
    }
}
