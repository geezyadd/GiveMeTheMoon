using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // A passage from the cruise start to station B; the field keeps it after generation.
    public sealed class FieldRoute {
        public int Id { get; }
        public float Difficulty { get; }
        public IReadOnlyList<Vector2Int> Centerline { get; }
        // Always odd: the centerline cell and the same number of cells on each side.
        public int CorridorWidthCells { get; }
        public int CorridorHalfWidthCells => (CorridorWidthCells - 1) / 2;
        public Vector2Int Start => Centerline[0];
        public Vector2Int End => Centerline[Centerline.Count - 1];

        public FieldRoute(int id, float difficulty, IReadOnlyList<Vector2Int> centerline, int corridorWidthCells) {
            Id = id;
            Difficulty = difficulty;
            Centerline = centerline;
            CorridorWidthCells = corridorWidthCells;
        }
    }
}
