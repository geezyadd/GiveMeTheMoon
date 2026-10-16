using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // A dense field of rocks over a footprint: the footprint (a full rectangle, else every cell) is cut into parts,
    // halving a long part until it is about square, and every part gets one rock at a jittered spot, turned at random
    // in 3D and sized so its mesh bounds cover the part apart from a gap at its edge (two neighbours leave at most the
    // recipe's largest gap between them), grown by a random share as far as the spill allows. A part whose rock would
    // reach farther past the footprint than the spill is split into four, so big rocks stand inside and smaller ones
    // along the edge. Extra
    // rocks stand above and below the plane over the footprint. Offsets are in field space from the middle of the
    // footprint box, like the cluster layout's.
    public sealed class FieldPlaneCoverLayout : IFieldPlaneCoverLayout {
        private const float HALF = 0.5f;
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;
        // A part longer than this many times its width is halved along its length first.
        private const float MAX_PART_ASPECT = 1.5f;
        // The farthest vertex of any recipe rock from its pivot, as a share of its size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;

        public void Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            if (!recipe.HasPlaneCover)
                return;

            Vector2 boxHalf = new(footprint.Height * cellSize * HALF, footprint.Width * cellSize * HALF);
            bool isFullRectangle = footprint.Cells.Count == footprint.Width * footprint.Height;
            if (isFullRectangle)
                CoverPart(recipe, new Rect(-boxHalf, boxHalf * 2f), boxHalf, random, rocks);
            else
                CoverCells(recipe, footprint, cellSize, boxHalf, random, rocks);

            AddExtraRocks(recipe, footprint, cellSize, random, rocks);
        }

        private void CoverCells(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, Vector2 boxHalf, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            foreach (Vector2Int cell in footprint.Cells)
                CoverPart(recipe, new Rect(new Vector2(cell.y * cellSize, cell.x * cellSize) - boxHalf, Vector2.one * cellSize), boxHalf, random, rocks);
        }

        // Part: x across the field, y along it, in meters from the middle of the footprint box.
        private void CoverPart(FieldClusterRecipe recipe, Rect part, Vector2 boxHalf, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            float longer = Mathf.Max(part.width, part.height);
            float shorter = Mathf.Min(part.width, part.height);
            if (longer > shorter * MAX_PART_ASPECT) {
                SplitAlongLongerSide(recipe, part, boxHalf, random, rocks);
                return;
            }

            float gap = Mathf.Lerp(recipe.PlaneGapMeters.x, recipe.PlaneGapMeters.y, random.Value());
            float needed = Mathf.Max((longer * HALF + recipe.PlaneJitterMeters - gap * HALF) / recipe.PlaneHalfExtentShare,
                recipe.PlaneMinHalfHeightMeters / recipe.PlaneHalfExtentShare);
            float growth = 1f + recipe.PlaneSizeVariation * random.Value();
            float toEdge = Mathf.Min(boxHalf.x - Mathf.Abs(part.center.x), boxHalf.y - Mathf.Abs(part.center.y));
            float largestFitting = (toEdge + recipe.PlaneMaxSpillMeters - recipe.PlaneJitterMeters) / recipe.PlaneReachShare;
            bool isSplittable = shorter * HALF >= recipe.PlaneMinPartMeters;
            if (needed > largestFitting && isSplittable) {
                SplitInFour(recipe, part, boxHalf, random, rocks);
                return;
            }

            rocks.Add(CreateRock(recipe, part.center, Mathf.Max(needed, Mathf.Min(needed * growth, largestFitting)), random));
        }

        private void SplitAlongLongerSide(FieldClusterRecipe recipe, Rect part, Vector2 boxHalf, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            bool isAcross = part.width >= part.height;
            Vector2 halfSize = isAcross ? new Vector2(part.width * HALF, part.height) : new Vector2(part.width, part.height * HALF);
            Vector2 step = isAcross ? new Vector2(halfSize.x, 0f) : new Vector2(0f, halfSize.y);
            CoverPart(recipe, new Rect(part.min, halfSize), boxHalf, random, rocks);
            CoverPart(recipe, new Rect(part.min + step, halfSize), boxHalf, random, rocks);
        }

        private void SplitInFour(FieldClusterRecipe recipe, Rect part, Vector2 boxHalf, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            Vector2 quarter = part.size * HALF;
            for (int across = 0; across < 2; across++) {
                for (int along = 0; along < 2; along++)
                    CoverPart(recipe, new Rect(part.min + new Vector2(across * quarter.x, along * quarter.y), quarter), boxHalf, random, rocks);
            }
        }

        private static FieldClusterRockPose CreateRock(FieldClusterRecipe recipe, Vector2 center, float size, IFieldRandom random) {
            FieldRockVisual prefab = FieldClusterLayout.PickRock(recipe.PlaneRocks, random);
            Material material = recipe.Materials[random.Range(0, recipe.Materials.Count)];
            Vector3 offset = new(center.x + Spread(recipe.PlaneJitterMeters, random), Spread(recipe.PlaneHeightJitterMeters, random), center.y + Spread(recipe.PlaneJitterMeters, random));
            return new FieldClusterRockPose(prefab, material, offset, FieldClusterLayout.RandomRotation(random), Vector3.one * size, true);
        }

        // Over random cells of the footprint, high or low enough that most of them stay off the plane, kept inside the
        // footprint by their own reach.
        private static void AddExtraRocks(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            float wanted = footprint.Cells.Count * recipe.ExtraRocksPerCell;
            int count = Mathf.FloorToInt(wanted) + (random.Value() < wanted - Mathf.Floor(wanted) ? 1 : 0);
            for (int i = 0; i < count; i++)
                rocks.Add(CreateExtraRock(recipe, footprint, cellSize, random));
        }

        private static FieldClusterRockPose CreateExtraRock(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random) {
            Vector2Int cell = footprint.Cells[random.Range(0, footprint.Cells.Count)];
            float diameter = Mathf.Lerp(recipe.ExtraDiameterMeters.x, recipe.ExtraDiameterMeters.y, random.Value());
            float side = random.Value() < HALF ? -1f : 1f;
            float height = side * Mathf.Lerp(recipe.ExtraHeightMeters.x, recipe.ExtraHeightMeters.y, random.Value());
            Vector3 cellCenter = new((cell.y + HALF - footprint.Height * HALF) * cellSize, height, (cell.x + HALF - footprint.Width * HALF) * cellSize);
            Vector3 offset = cellCenter + new Vector3(Spread(cellSize * HALF, random), 0f, Spread(cellSize * HALF, random));
            FieldRockVisual prefab = FieldClusterLayout.PickRock(recipe.Rocks, random);
            Material material = recipe.Materials[random.Range(0, recipe.Materials.Count)];
            float size = diameter * HALF / ROCK_REACH;
            return new FieldClusterRockPose(prefab, material, offset, FieldClusterLayout.RandomRotation(random), Vector3.one * size);
        }

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());
    }
}
