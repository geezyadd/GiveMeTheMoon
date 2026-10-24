using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // A course through the field; the field keeps it after generation. The main route runs from the cruise start to
    // station B.
    public sealed class FieldRoute {
        public int Id { get; }
        public FieldRouteKind Kind { get; }
        public float Difficulty { get; }
        public IReadOnlyList<Vector2Int> Centerline { get; }
        // Cells on each side of the centerline, one value per centerline cell (0 for the straight main line).
        public IReadOnlyList<int> HalfWidths { get; }
        // Always odd: the narrowest place of the route, the centerline cell and the same number of cells on each side.
        public int CorridorWidthCells { get; }
        public Vector2Int Start => Centerline[0];
        public Vector2Int End => Centerline[Centerline.Count - 1];

        public FieldRoute(int id, FieldRouteKind kind, float difficulty, IReadOnlyList<Vector2Int> centerline, IReadOnlyList<int> halfWidths) {
            Id = id;
            Kind = kind;
            Difficulty = difficulty;
            Centerline = centerline;
            HalfWidths = halfWidths;
            CorridorWidthCells = MinHalfWidth(halfWidths) * 2 + 1;
        }

        private static int MinHalfWidth(IReadOnlyList<int> halfWidths) {
            int min = int.MaxValue;
            foreach (int halfWidth in halfWidths)
                min = Mathf.Min(min, halfWidth);

            return min == int.MaxValue ? 0 : min;
        }
    }
}
