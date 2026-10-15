using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Finds the cells of a corridor around a centerline; the row order keeps generation deterministic.
    public sealed class FieldCorridorMapper : IFieldCorridorMapper {
        public List<Vector2Int> Map(int sideCells, IReadOnlyList<Vector2Int> centerline, int halfWidthCells) {
            bool[,] isInCorridor = new bool[sideCells, sideCells];
            foreach (Vector2Int cell in centerline)
                MarkSquare(isInCorridor, cell, halfWidthCells, sideCells);

            List<Vector2Int> corridor = new();
            for (int y = 0; y < sideCells; y++)
                CollectRow(isInCorridor, y, sideCells, corridor);

            return corridor;
        }

        private static void CollectRow(bool[,] isInCorridor, int y, int sideCells, List<Vector2Int> corridor) {
            for (int x = 0; x < sideCells; x++) {
                if (isInCorridor[x, y])
                    corridor.Add(new Vector2Int(x, y));
            }
        }

        private static void MarkSquare(bool[,] isInCorridor, Vector2Int center, int halfWidthCells, int sideCells) {
            int minX = Mathf.Max(0, center.x - halfWidthCells);
            int maxX = Mathf.Min(sideCells - 1, center.x + halfWidthCells);
            int minY = Mathf.Max(0, center.y - halfWidthCells);
            int maxY = Mathf.Min(sideCells - 1, center.y + halfWidthCells);
            for (int y = minY; y <= maxY; y++) {
                for (int x = minX; x <= maxX; x++)
                    isInCorridor[x, y] = true;
            }
        }
    }
}
