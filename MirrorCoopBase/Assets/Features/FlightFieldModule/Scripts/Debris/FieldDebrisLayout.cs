using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // Lays out the small rocks of one debris cell: a cell of density d tries d times the recipe's rock count (the
    // fraction by chance); every rock keeps half the minimum gap from the cell's edge and the whole gap from the other
    // rocks of the cell, so the ship always fits between two rocks, also across the edge of two cells. A rock that
    // finds no spot in its tries is dropped.
    public sealed class FieldDebrisLayout : IFieldDebrisLayout {
        private const float HALF = 0.5f;
        private const uint CELL_X_PRIME = 0x8DA6B343u;
        private const uint CELL_Y_PRIME = 0xD8163841u;
        private const uint LOOK_SALT = 0xCB1AB31Fu;

        public void Fill(FieldDebrisRecipe recipe, float density, int fieldSeed, Vector2Int cell, float cellSize, float minGap, IFieldRandom random,
                         List<FieldDebrisSpot> spots) {
            spots.Clear();
            random.Reseed(CellSeed(fieldSeed, cell));
            float wanted = Mathf.Clamp01(density) * recipe.MaxRocksPerCell;
            int count = Mathf.FloorToInt(wanted) + (random.Value() < wanted - Mathf.Floor(wanted) ? 1 : 0);
            for (int i = 0; i < count; i++)
                TryAddRock(recipe, cellSize, minGap, random, spots);
        }

        public int LookSeed(int fieldSeed, Vector2Int cell) =>
            unchecked((int)((uint)CellSeed(fieldSeed, cell) ^ LOOK_SALT));

        private static void TryAddRock(FieldDebrisRecipe recipe, float cellSize, float minGap, IFieldRandom random, List<FieldDebrisSpot> spots) {
            for (int attempt = 0; attempt < recipe.PlacementTries; attempt++) {
                float diameter = Mathf.Lerp(recipe.DiameterMeters.x, recipe.DiameterMeters.y, random.Value());
                float reach = cellSize * HALF - minGap * HALF - diameter * HALF;
                if (reach < 0f)
                    continue;

                Vector3 offset = new(Spread(reach, random), Spread(recipe.HeightJitterMeters, random), Spread(reach, random));
                if (!IsClearOfOthers(offset, diameter, minGap, spots))
                    continue;

                spots.Add(new FieldDebrisSpot(offset, diameter));
                return;
            }
        }

        private static bool IsClearOfOthers(Vector3 offset, float diameter, float minGap, List<FieldDebrisSpot> spots) {
            Vector2 onPlane = new(offset.x, offset.z);
            foreach (FieldDebrisSpot spot in spots) {
                float gap = Vector2.Distance(onPlane, new Vector2(spot.Offset.x, spot.Offset.z)) - diameter * HALF - spot.Radius;
                if (gap < minGap)
                    return false;
            }

            return true;
        }

        private static int CellSeed(int fieldSeed, Vector2Int cell) =>
            unchecked((int)((uint)fieldSeed ^ (uint)cell.x * CELL_X_PRIME ^ (uint)cell.y * CELL_Y_PRIME));

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());
    }
}
