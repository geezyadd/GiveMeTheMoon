using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Where the rocks of one cluster stand. Everything comes from the given random sequence, so the same seed gives
    // the same rocks on every peer. Offsets are in field space from the middle of the footprint box: x across the
    // field (box height), z along it (box width), y up from the flight plane.
    public sealed class FieldClusterLayout : IFieldClusterLayout {
        private const float HALF = 0.5f;
        // The farthest vertex of a rock prefab from its pivot, as a share of the rock's size (the pack's rocks reach 0.69
        // when normalised to 1 m): the bound that keeps a turned rock inside the footprint.
        private const float ROCK_REACH = 0.7f;
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;

        public void Fill(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            rocks.Clear();
            AddMainRock(recipe, footprint, cellSize, random, rocks);
            int satellites = random.Range(recipe.SatelliteCount.x, recipe.SatelliteCount.y + 1);
            for (int i = 0; i < satellites; i++)
                rocks.Add(CreateCellRock(recipe, footprint, cellSize, recipe.SatelliteSize, random));

            if (random.Value() >= recipe.DebrisChance)
                return;

            int debris = random.Range(recipe.DebrisCount.x, recipe.DebrisCount.y + 1);
            for (int i = 0; i < debris; i++)
                rocks.Add(CreateCellRock(recipe, footprint, cellSize, recipe.DebrisSize, random));
        }

        // Stays within a tenth of its size from the plane: the thinnest rock is over a third as thick as it is wide, so
        // even at the smallest axis scale it still crosses the plane.
        private static void AddMainRock(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            float shorterSide = Mathf.Min(footprint.Width, footprint.Height) * cellSize;
            float size = Mathf.Min(recipe.MainSize * shorterSide, LargestSizeInside(shorterSide, recipe));
            float height = Mathf.Lerp(-recipe.MainHeightJitter, recipe.MainHeightJitter, random.Value()) * size;
            rocks.Add(CreateRock(recipe, new Vector3(0f, height, 0f), size, random));
        }

        // A rock over a random cell of the footprint, kept inside that cell: the client does not know the corridor
        // cells, so a rock must not reach into a neighbouring one.
        private static FieldClusterRockPose CreateCellRock(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, Vector2 sizeRange, IFieldRandom random) {
            Vector2Int cell = footprint.Cells[random.Range(0, footprint.Cells.Count)];
            float size = Mathf.Min(Mathf.Lerp(sizeRange.x, sizeRange.y, random.Value()) * cellSize, LargestSizeInside(cellSize, recipe));
            float radius = size * recipe.AxisScale.y * ROCK_REACH;
            float reach = cellSize * HALF - radius;
            Vector3 cellCenter = new((cell.y + HALF - footprint.Height * HALF) * cellSize, 0f, (cell.x + HALF - footprint.Width * HALF) * cellSize);
            Vector3 offset = cellCenter + new Vector3(Spread(reach, random), Spread(recipe.HeightSpreadCells * cellSize, random), Spread(reach, random));
            return CreateRock(recipe, offset, size, random);
        }

        private static FieldClusterRockPose CreateRock(FieldClusterRecipe recipe, Vector3 offset, float size, IFieldRandom random) {
            FieldRockVisual prefab = PickPrefab(recipe, random);
            Material material = recipe.Materials[random.Range(0, recipe.Materials.Count)];
            Quaternion rotation = RandomRotation(random);
            Vector3 scale = new(AxisScale(recipe, random) * size, AxisScale(recipe, random) * size, AxisScale(recipe, random) * size);
            return new FieldClusterRockPose(prefab, material, offset, rotation, scale);
        }

        internal static FieldRockVisual PickPrefab(FieldClusterRecipe recipe, IFieldRandom random) =>
            FieldWeightedPick.Pick(recipe.Rocks, random).Prefab;

        // The largest rock that, turned any way and stretched by the recipe's largest axis scale, stays inside a box of
        // the given side around its pivot.
        private static float LargestSizeInside(float extent, FieldClusterRecipe recipe) =>
            extent * HALF / (ROCK_REACH * recipe.AxisScale.y);

        private static float AxisScale(FieldClusterRecipe recipe, IFieldRandom random) =>
            Mathf.Lerp(recipe.AxisScale.x, recipe.AxisScale.y, random.Value());

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());

        // Uniform over all orientations (Shoemake): an asteroid has no up.
        internal static Quaternion RandomRotation(IFieldRandom random) {
            float split = random.Value();
            float firstAngle = random.Value() * FULL_TURN_RADIANS;
            float secondAngle = random.Value() * FULL_TURN_RADIANS;
            float firstRadius = Mathf.Sqrt(1f - split);
            float secondRadius = Mathf.Sqrt(split);
            return new Quaternion(firstRadius * Mathf.Sin(firstAngle), firstRadius * Mathf.Cos(firstAngle), secondRadius * Mathf.Sin(secondAngle),
                                  secondRadius * Mathf.Cos(secondAngle));
        }
    }
}
