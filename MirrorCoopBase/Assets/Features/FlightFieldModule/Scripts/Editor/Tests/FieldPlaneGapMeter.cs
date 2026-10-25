using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The smallest gap in the flight plane of an obstacle field: between two debris rocks crossing the plane, between
    // such a rock and a giant (its footprint cells, which its rock stays inside) and between two giants (footprint
    // cells of two placements that touch, also diagonally, leave no gap; otherwise at least a cell).
    public sealed class FieldPlaneGapMeter {
        private readonly Dictionary<Vector2Int, List<FieldSlalomRock>> _rocksByCell = new();

        public float Measure(FlightField field, IReadOnlyList<FieldSlalomRock> rocks) {
            float cellSize = field.Frame.CellSize;
            _rocksByCell.Clear();
            float largestRadius = 0f;
            foreach (FieldSlalomRock rock in rocks) {
                if (!rock.IsInPlane)
                    continue;

                Vector2Int cell = FieldPlaneCells.ToCell(rock.Position, cellSize);
                if (!_rocksByCell.TryGetValue(cell, out List<FieldSlalomRock> inCell)) {
                    inCell = new List<FieldSlalomRock>();
                    _rocksByCell.Add(cell, inCell);
                }

                inCell.Add(rock);
                largestRadius = Mathf.Max(largestRadius, rock.Radius);
            }

            float gap = Mathf.Min(MinGiantGap(field), cellSize);
            foreach (FieldSlalomRock rock in rocks) {
                if (rock.IsInPlane)
                    gap = Mathf.Min(gap, Mathf.Min(MinRockGap(rock, cellSize, largestRadius), MinGiantClearance(field, rock)));
            }

            return gap;
        }

        private float MinRockGap(FieldSlalomRock rock, float cellSize, float largestRadius) {
            Vector2Int center = FieldPlaneCells.ToCell(rock.Position, cellSize);
            int reach = Mathf.CeilToInt((rock.Radius + largestRadius + cellSize) / cellSize);
            float gap = float.PositiveInfinity;
            for (int x = center.x - reach; x <= center.x + reach; x++) {
                for (int y = center.y - reach; y <= center.y + reach; y++)
                    gap = Mathf.Min(gap, MinGapInCell(rock, new Vector2Int(x, y)));
            }

            return gap;
        }

        private float MinGapInCell(FieldSlalomRock rock, Vector2Int cell) {
            if (!_rocksByCell.TryGetValue(cell, out List<FieldSlalomRock> others))
                return float.PositiveInfinity;

            float gap = float.PositiveInfinity;
            foreach (FieldSlalomRock other in others) {
                bool isSameRock = other.Position == rock.Position && Mathf.Approximately(other.Radius, rock.Radius);
                if (!isSameRock)
                    gap = Mathf.Min(gap, Vector2.Distance(rock.Position, other.Position) - rock.Radius - other.Radius);
            }

            return gap;
        }

        private static float MinGiantClearance(FlightField field, FieldSlalomRock rock) {
            float cellSize = field.Frame.CellSize;
            Vector2Int center = FieldPlaneCells.ToCell(rock.Position, cellSize);
            int reach = Mathf.CeilToInt(rock.Radius / cellSize) + 1;
            float gap = float.PositiveInfinity;
            for (int x = center.x - reach; x <= center.x + reach; x++) {
                for (int y = center.y - reach; y <= center.y + reach; y++) {
                    Vector2Int cell = new(x, y);
                    if (field.Cells.ValidateIndex(cell) && field.Cells[cell].IsOccupied)
                        gap = Mathf.Min(gap, FieldPlaneCells.DistanceToCell(rock.Position, cell, cellSize) - rock.Radius);
                }
            }

            return gap;
        }

        private static float MinGiantGap(FlightField field) {
            foreach (Vector2Int cell in field.Cells.GetIndexes(IsOccupied)) {
                if (TouchesOtherGiant(field, cell))
                    return 0f;
            }

            return float.PositiveInfinity;
        }

        private static bool TouchesOtherGiant(FlightField field, Vector2Int cell) {
            int placement = field.Cells[cell].PlacementId;
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    Vector2Int neighbour = cell + new Vector2Int(x, y);
                    if (field.Cells.ValidateIndex(neighbour) && field.Cells[neighbour].IsOccupied && field.Cells[neighbour].PlacementId != placement)
                        return true;
                }
            }

            return false;
        }

        private static bool IsOccupied(FieldCell cell) =>
            cell.IsOccupied;
    }
}
