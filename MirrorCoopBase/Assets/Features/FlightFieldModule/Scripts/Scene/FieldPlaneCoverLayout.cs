using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Closes the flight plane over a footprint with one or two giants: a full rectangular footprint is one block, or
    // two blocks side by side when it is long enough and the recipe's chance says so (any other shape: one block per
    // cell). Each block gets one chunky rock turned by quarter turns plus a little jitter and stretched so its mesh
    // bounds cover the block apart from a gap at its edge (two neighbours leave at most the recipe's largest gap
    // between them), sticking out past the block by a random protrusion, and rising far above and below the plane by
    // a height that follows the block's size. Offsets are in field space from the middle of the footprint box, like
    // the cluster layout's.
    public sealed class FieldPlaneCoverLayout : IFieldPlaneCoverLayout {
        private const float HALF = 0.5f;
        private const float QUARTER_TURN_DEGREES = 90f;
        private const int QUARTER_TURNS = 4;
        // A middle moves at most this share of the half height up or down, so the plane stays deep inside the rock.
        private const float MAX_HEIGHT_JITTER_SHARE = 0.5f;

        // Reused per call: building runs every frame of the takeoff.
        private readonly List<RectInt> _blocks = new();

        public float Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            if (!recipe.HasPlaneCover)
                return 0f;

            FillBlocks(recipe, footprint, random);
            float lowestHalfHeight = float.PositiveInfinity;
            foreach (RectInt block in _blocks)
                lowestHalfHeight = Mathf.Min(lowestHalfHeight, AddGiant(recipe, footprint, cellSize, block, random, rocks));

            return lowestHalfHeight;
        }

        // Blocks are in footprint cells: x along the field (box width), y across it (box height).
        private void FillBlocks(FieldClusterRecipe recipe, FieldFootprint footprint, IFieldRandom random) {
            _blocks.Clear();
            bool isFullRectangle = footprint.Cells.Count == footprint.Width * footprint.Height;
            if (!isFullRectangle) {
                foreach (Vector2Int cell in footprint.Cells)
                    _blocks.Add(new RectInt(cell.x, cell.y, 1, 1));

                return;
            }

            int longer = Mathf.Max(footprint.Width, footprint.Height);
            bool isSplit = longer >= recipe.PlaneSplitMinCells && random.Value() < recipe.PlaneSplitChance;
            if (!isSplit) {
                _blocks.Add(new RectInt(0, 0, footprint.Width, footprint.Height));
                return;
            }

            int cut = random.Range(longer / 2 - (longer - 1) / 4, longer / 2 + (longer - 1) / 4 + 1);
            bool isAlong = footprint.Width >= footprint.Height;
            _blocks.Add(isAlong ? new RectInt(0, 0, cut, footprint.Height) : new RectInt(0, 0, footprint.Width, cut));
            _blocks.Add(isAlong ? new RectInt(cut, 0, footprint.Width - cut, footprint.Height) : new RectInt(0, cut, footprint.Width, footprint.Height - cut));
        }

        // Returns the giant's half height.
        private static float AddGiant(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, RectInt block, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            Vector3 center = new((block.y + block.height * HALF - footprint.Height * HALF) * cellSize, 0f, (block.x + block.width * HALF - footprint.Width * HALF) * cellSize);
            float edge = recipe.PlaneJitterMeters + Lerp(recipe.PlaneProtrusionMeters, random) - Lerp(recipe.PlaneGapMeters, random) * HALF;
            Vector2 needed = new(block.height * cellSize * HALF + edge, block.width * cellSize * HALF + edge);
            int quarter = random.Range(0, QUARTER_TURNS);
            float yaw = quarter * QUARTER_TURN_DEGREES + Spread(recipe.PlaneYawJitterDegrees, random);
            Vector2 localNeeded = quarter % 2 == 0 ? needed : new Vector2(needed.y, needed.x);
            Vector2 horizontalScale = SolveHorizontalScale(recipe, localNeeded);
            float halfHeight = GetHalfHeight(recipe, Mathf.Min(block.width, block.height) * cellSize, random);
            float height = Spread(Mathf.Min(recipe.PlaneHeightJitterMeters, halfHeight * MAX_HEIGHT_JITTER_SHARE), random);
            Vector3 offset = center + new Vector3(Spread(recipe.PlaneJitterMeters, random), height, Spread(recipe.PlaneJitterMeters, random));
            Vector3 scale = new(horizontalScale.x, halfHeight / recipe.PlaneHalfHeightShare, horizontalScale.y);
            FieldRockVisual prefab = FieldClusterLayout.PickRock(recipe.PlaneRocks, random);
            Material material = recipe.Materials[random.Range(0, recipe.Materials.Count)];
            rocks.Add(new FieldClusterRockPose(prefab, material, offset, Quaternion.Euler(0f, yaw, 0f), scale, true));
            return halfHeight;
        }

        // The local x and z scales whose bounds reach the needed extents along the world axes they turn to: at a yaw
        // jitter of up to J, an extent loses up to cos J of itself and the other axis swings in by up to sin J of its
        // reach, so x * kept - z * swung >= needed x and z * kept - x * swung >= needed z, with kept = half extent * cos J
        // and swung = reach * sin J.
        private static Vector2 SolveHorizontalScale(FieldClusterRecipe recipe, Vector2 needed) {
            float jitter = recipe.PlaneYawJitterDegrees * Mathf.Deg2Rad;
            float kept = recipe.PlaneHalfExtentShare * Mathf.Cos(jitter);
            float swung = recipe.PlaneReachShare * Mathf.Sin(jitter);
            float determinant = kept * kept - swung * swung;
            return new Vector2((kept * needed.x + swung * needed.y) / determinant, (kept * needed.y + swung * needed.x) / determinant);
        }

        private static float GetHalfHeight(FieldClusterRecipe recipe, float shorterSide, IFieldRandom random) {
            Vector2 range = recipe.PlaneHalfHeightMeters;
            float baseHeight = Mathf.Min(Mathf.Clamp(shorterSide * recipe.PlaneHalfHeightPerMeter, range.x, range.y), shorterSide * recipe.PlaneMaxHeightToWidth);
            return baseHeight * (1f + Spread(recipe.PlaneHeightVariation, random));
        }

        private static float Lerp(Vector2 range, IFieldRandom random) =>
            Mathf.Lerp(range.x, range.y, random.Value());

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());
    }
}
