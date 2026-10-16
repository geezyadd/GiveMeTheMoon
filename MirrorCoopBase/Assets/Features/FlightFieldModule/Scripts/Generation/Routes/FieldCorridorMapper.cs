using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Finds the cells of a corridor around a centerline; the row order keeps generation deterministic.
    public sealed class FieldCorridorMapper : IFieldCorridorMapper {
        public List<Vector2Int> Map(Vector2Int size, IReadOnlyList<Vector2Int> centerline, int halfWidthCells) {
            bool[,] isInCorridor = new bool[size.x, size.y];
            foreach (Vector2Int cell in centerline)
                MarkSquare(isInCorridor, cell, halfWidthCells, size);

            List<Vector2Int> corridor = new();
            for (int y = 0; y < size.y; y++)
                CollectRow(isInCorridor, y, size.x, corridor);

            return corridor;
        }

        private static void CollectRow(bool[,] isInCorridor, int y, int lengthCells, List<Vector2Int> corridor) {
            for (int x = 0; x < lengthCells; x++) {
                if (isInCorridor[x, y])
                    corridor.Add(new Vector2Int(x, y));
            }
        }

        private static void MarkSquare(bool[,] isInCorridor, Vector2Int center, int halfWidthCells, Vector2Int size) {
            int minX = Mathf.Max(0, center.x - halfWidthCells);
            int maxX = Mathf.Min(size.x - 1, center.x + halfWidthCells);
            int minY = Mathf.Max(0, center.y - halfWidthCells);
            int maxY = Mathf.Min(size.y - 1, center.y + halfWidthCells);
            for (int y = minY; y <= maxY; y++) {
                for (int x = minX; x <= maxX; x++)
                    isInCorridor[x, y] = true;
            }
        }
    }
}
