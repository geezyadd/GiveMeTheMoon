using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // A corridor through the field; the field keeps it after generation. A main route runs from the cruise start to
    // station B, an alternative from one main centerline cell to a later one, a dead end from a main centerline cell
    // into a ridge.
    public sealed class FieldRoute {
        public int Id { get; }
        public FieldRouteKind Kind { get; }
        public float Difficulty { get; }
        public IReadOnlyList<Vector2Int> Centerline { get; }
        // Cells on each side of the centerline, one value per centerline cell: the corridor's width changes along it.
        public IReadOnlyList<int> HalfWidths { get; }
        // Always odd: the narrowest place of the corridor, the centerline cell and the same number of cells on each side.
        public int CorridorWidthCells { get; }
        // An alternative route the generator closed with a ridge: it looks like a way but does not let the ship through.
        public bool IsClosed { get; }
        public int CorridorHalfWidthCells => (CorridorWidthCells - 1) / 2;
        public Vector2Int Start => Centerline[0];
        public Vector2Int End => Centerline[Centerline.Count - 1];

        public FieldRoute(int id, FieldRouteKind kind, float difficulty, IReadOnlyList<Vector2Int> centerline, IReadOnlyList<int> halfWidths, bool isClosed) {
            Id = id;
            Kind = kind;
            Difficulty = difficulty;
            Centerline = centerline;
            HalfWidths = halfWidths;
            IsClosed = isClosed;
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
