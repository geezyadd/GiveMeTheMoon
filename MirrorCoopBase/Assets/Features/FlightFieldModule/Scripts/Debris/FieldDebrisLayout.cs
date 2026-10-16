using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // Lays out the small rocks of one debris cell from a jittered grid over the whole field plane: every grid point
    // moves by a seeded jitter and stands as a rock with the chance of the density of the cell it lands in. The grid
    // spacing minus twice the jitter keeps any two rocks of the field, in one cell or two, at least the minimum gap
    // apart (no clumps, no overlaps), and a point depends only on the field seed and its grid coordinates, so every
    // peer gets the same rocks whatever order the cells are built in.
    public sealed class FieldDebrisLayout : IFieldDebrisLayout {
        private const float HALF = 0.5f;
        private const uint POINT_X_PRIME = 0x8DA6B343u;
        private const uint POINT_Y_PRIME = 0xD8163841u;
        private const uint LOOK_SALT = 0xCB1AB31Fu;

        public void Fill(FieldDebrisRecipe recipe, float density, int fieldSeed, Vector2Int cell, float cellSize, float minGap, IFieldRandom random,
                         List<FieldDebrisSpot> spots) {
            spots.Clear();
            float spacing = recipe.SpacingMeters;
            float jitter = GetJitter(recipe, minGap);
            Vector2 cellMin = new(cell.y * cellSize, cell.x * cellSize);
            Vector2 cellCenter = cellMin + Vector2.one * (cellSize * HALF);
            Vector2Int first = new(Mathf.FloorToInt((cellMin.x - jitter) / spacing), Mathf.FloorToInt((cellMin.y - jitter) / spacing));
            Vector2Int last = new(Mathf.FloorToInt((cellMin.x + cellSize + jitter) / spacing), Mathf.FloorToInt((cellMin.y + cellSize + jitter) / spacing));
            for (int across = first.x; across <= last.x; across++) {
                for (int along = first.y; along <= last.y; along++)
                    TryAddPoint(recipe, density, fieldSeed, new Vector2Int(across, along), jitter, cellMin, cellSize, cellCenter, random, spots);
            }
        }

        public int LookSeed(int fieldSeed, Vector2Int cell) =>
            unchecked((int)((uint)PointSeed(fieldSeed, cell) ^ LOOK_SALT));

        // The jitter that keeps the gap between the two largest rocks on neighbouring grid points.
        private static float GetJitter(FieldDebrisRecipe recipe, float minGap) =>
            Mathf.Max(0f, (recipe.SpacingMeters - minGap - recipe.DiameterMeters.y) * HALF);

        // Point: x across the field, y along it, in grid steps. The draws come in a fixed order, so the same point gives
        // the same rock in whichever cell it is checked.
        private static void TryAddPoint(FieldDebrisRecipe recipe, float density, int fieldSeed, Vector2Int point, float jitter, Vector2 cellMin, float cellSize,
                                        Vector2 cellCenter, IFieldRandom random, List<FieldDebrisSpot> spots) {
            random.Reseed(PointSeed(fieldSeed, point));
            Vector2 position = (point + Vector2.one * HALF) * recipe.SpacingMeters + new Vector2(Spread(jitter, random), Spread(jitter, random));
            float presence = random.Value();
            float diameter = Mathf.Lerp(recipe.DiameterMeters.x, recipe.DiameterMeters.y, random.Value());
            float height = Spread(recipe.HeightJitterMeters, random);
            bool isInCell = position.x >= cellMin.x && position.x < cellMin.x + cellSize && position.y >= cellMin.y && position.y < cellMin.y + cellSize;
            if (isInCell && presence < density)
                spots.Add(new FieldDebrisSpot(new Vector3(position.x - cellCenter.x, height, position.y - cellCenter.y), diameter));
        }

        private static int PointSeed(int fieldSeed, Vector2Int point) =>
            unchecked((int)((uint)fieldSeed ^ (uint)point.x * POINT_X_PRIME ^ (uint)point.y * POINT_Y_PRIME));

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());
    }
}
