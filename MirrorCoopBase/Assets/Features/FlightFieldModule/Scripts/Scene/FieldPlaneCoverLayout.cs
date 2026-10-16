using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Closes the flight plane over a footprint: the cells are covered greedily by squares of cells, the largest first,
    // one chunky rock per square, turned around the vertical only and sized so its mesh bounds reach past the square by
    // the recipe's overlap. A square of more than one cell is used only when its rock stays within the spill of the
    // footprint, so big footprints get a few huge rocks inside and single-cell rocks along their edge. Offsets are in
    // field space from the middle of the footprint box, like the cluster layout's.
    public sealed class FieldPlaneCoverLayout : IFieldPlaneCoverLayout {
        private const float HALF = 0.5f;
        private const float FULL_TURN_DEGREES = 360f;
        // The farthest vertex of a rock prefab from its pivot, as a share of the rock's size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;

        // Reused per call: building runs every frame of the takeoff and allocates nothing once the arrays are big enough.
        private bool[,] _isFootprintCell = new bool[0, 0];
        private bool[,] _isCovered = new bool[0, 0];

        public void Add(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            if (!recipe.HasPlaneCover)
                return;

            PrepareCells(footprint);
            int largestBlock = Mathf.Min(recipe.PlaneMaxBlockCells, Mathf.Min(footprint.Width, footprint.Height));
            for (int block = largestBlock; block >= 1; block--)
                CoverWithBlocks(recipe, footprint, cellSize, block, random, rocks);
        }

        private void CoverWithBlocks(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, int block, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            for (int y = 0; y + block <= footprint.Height; y++) {
                for (int x = 0; x + block <= footprint.Width; x++)
                    TryCoverBlock(recipe, footprint, cellSize, new Vector2Int(x, y), block, random, rocks);
            }
        }

        private void TryCoverBlock(FieldClusterRecipe recipe, FieldFootprint footprint, float cellSize, Vector2Int corner, int block, IFieldRandom random,
                                   List<FieldClusterRockPose> rocks) {
            if (!IsOpenBlock(corner, block))
                return;

            Vector3 center = new((corner.y + block * HALF - footprint.Height * HALF) * cellSize, 0f, (corner.x + block * HALF - footprint.Width * HALF) * cellSize);
            float size = (block * cellSize * HALF + recipe.PlaneOverlapMeters + recipe.PlaneJitterMeters) / (recipe.PlaneHalfExtentShare * recipe.PlaneAxisScale.x);
            float reach = size * ROCK_REACH * recipe.PlaneAxisScale.y;
            bool isSpillTooFar = block > 1 && reach > DistanceToBoxEdge(center, footprint, cellSize) + recipe.PlaneMaxSpillMeters;
            if (isSpillTooFar)
                return;

            MarkCovered(corner, block);
            rocks.Add(CreatePlaneRock(recipe, center, size, random));
        }

        private static FieldClusterRockPose CreatePlaneRock(FieldClusterRecipe recipe, Vector3 center, float size, IFieldRandom random) {
            FieldRockVisual prefab = FieldClusterLayout.PickRock(recipe.PlaneRocks, random);
            Material material = recipe.Materials[random.Range(0, recipe.Materials.Count)];
            Vector3 offset = center + new Vector3(Spread(recipe.PlaneJitterMeters, random), 0f, Spread(recipe.PlaneJitterMeters, random));
            Quaternion rotation = Quaternion.Euler(0f, random.Value() * FULL_TURN_DEGREES, 0f);
            Vector3 scale = new(AxisScale(recipe, random) * size, AxisScale(recipe, random) * size, AxisScale(recipe, random) * size);
            return new FieldClusterRockPose(prefab, material, offset, rotation, scale, true);
        }

        private void PrepareCells(FieldFootprint footprint) {
            bool isTooSmall = _isFootprintCell.GetLength(0) < footprint.Width || _isFootprintCell.GetLength(1) < footprint.Height;
            if (isTooSmall) {
                _isFootprintCell = new bool[footprint.Width, footprint.Height];
                _isCovered = new bool[footprint.Width, footprint.Height];
            }

            Array.Clear(_isFootprintCell, 0, _isFootprintCell.Length);
            Array.Clear(_isCovered, 0, _isCovered.Length);
            foreach (Vector2Int cell in footprint.Cells)
                _isFootprintCell[cell.x, cell.y] = true;
        }

        private bool IsOpenBlock(Vector2Int corner, int block) {
            for (int y = corner.y; y < corner.y + block; y++) {
                for (int x = corner.x; x < corner.x + block; x++) {
                    if (!_isFootprintCell[x, y] || _isCovered[x, y])
                        return false;
                }
            }

            return true;
        }

        private void MarkCovered(Vector2Int corner, int block) {
            for (int y = corner.y; y < corner.y + block; y++) {
                for (int x = corner.x; x < corner.x + block; x++)
                    _isCovered[x, y] = true;
            }
        }

        private static float DistanceToBoxEdge(Vector3 center, FieldFootprint footprint, float cellSize) =>
            Mathf.Min(footprint.Height * HALF * cellSize - Mathf.Abs(center.x), footprint.Width * HALF * cellSize - Mathf.Abs(center.z));

        private static float AxisScale(FieldClusterRecipe recipe, IFieldRandom random) =>
            Mathf.Lerp(recipe.PlaneAxisScale.x, recipe.PlaneAxisScale.y, random.Value());

        private static float Spread(float extent, IFieldRandom random) =>
            Mathf.Lerp(-extent, extent, random.Value());
    }
}
