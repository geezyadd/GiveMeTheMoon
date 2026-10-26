using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // What stands in the flight plane: the closed cells (the giants, one rock inside their footprint), the cells outside
    // the matrix, and the debris rocks that cross the plane, kept per cell for the nearest-obstacle lookup.
    public sealed class FieldSlalomObstacles {
        private readonly IMatrix<FieldCell> _cells;
        private readonly float _cellSize;
        private readonly Vector2Int _size;
        // Per matrix cell (index x * width + y), null where no rock stands.
        private readonly List<FieldSlalomRock>[] _rocksByCell;
        private readonly List<FieldSlalomRock> _planeRocks = new();
        private float _largestRadius;

        public Vector2Int Size => _size;
        public float CellSize => _cellSize;
        public IReadOnlyList<FieldSlalomRock> PlaneRocks => _planeRocks;

        public FieldSlalomObstacles(IMatrix<FieldCell> cells, Vector2Int size, float cellSize) {
            _cells = cells;
            _size = size;
            _rocksByCell = new List<FieldSlalomRock>[size.x * size.y];
            _cellSize = cellSize;
        }

        public void Add(FieldSlalomRock rock) {
            if (!rock.IsInPlane)
                return;

            Vector2Int cell = ToCell(rock.Position);
            if (!_cells.ValidateIndex(cell))
                return;

            int index = Index(cell);
            _rocksByCell[index] ??= new List<FieldSlalomRock>();
            _rocksByCell[index].Add(rock);
            _planeRocks.Add(rock);
            _largestRadius = Mathf.Max(_largestRadius, rock.Radius);
        }

        // Distance from the point to the nearest obstacle surface; anything farther than the limit counts as the limit.
        public float Clearance(Vector2 point, float limit) {
            Vector2Int center = ToCell(point);
            if (IsClosed(center))
                return 0f;

            float clearance = Mathf.Min(limit, WallClearance(point, center, limit));
            return Mathf.Min(clearance, RockClearance(point, center, limit));
        }

        public bool IsClosed(Vector2Int cell) =>
            !_cells.ValidateIndex(cell) || _cells[cell].IsOccupied;

        private float WallClearance(Vector2 point, Vector2Int center, float limit) {
            int reach = Mathf.CeilToInt(limit / _cellSize);
            float clearance = limit;
            for (int along = center.x - reach; along <= center.x + reach; along++) {
                for (int across = center.y - reach; across <= center.y + reach; across++) {
                    Vector2Int cell = new(along, across);
                    if (IsClosed(cell))
                        clearance = Mathf.Min(clearance, DistanceToCell(point, cell));
                }
            }

            return clearance;
        }

        private float RockClearance(Vector2 point, Vector2Int center, float limit) {
            int reach = Mathf.CeilToInt((_largestRadius + limit) / _cellSize);
            float clearance = limit;
            for (int along = center.x - reach; along <= center.x + reach; along++) {
                for (int across = center.y - reach; across <= center.y + reach; across++) {
                    Vector2Int cell = new(along, across);
                    if (_cells.ValidateIndex(cell) && _rocksByCell[Index(cell)] != null)
                        clearance = Mathf.Min(clearance, NearestSurface(point, _rocksByCell[Index(cell)]));
                }
            }

            return clearance;
        }

        private static float NearestSurface(Vector2 point, List<FieldSlalomRock> rocks) {
            float nearest = float.PositiveInfinity;
            foreach (FieldSlalomRock rock in rocks)
                nearest = Mathf.Min(nearest, Vector2.Distance(point, rock.Position) - rock.Radius);

            return nearest;
        }

        // Field plane: x across, y along; the cell is the matrix one (x along, y across).
        private float DistanceToCell(Vector2 point, Vector2Int cell) {
            Vector2 min = new(cell.y * _cellSize, cell.x * _cellSize);
            Vector2 nearest = new(Mathf.Clamp(point.x, min.x, min.x + _cellSize), Mathf.Clamp(point.y, min.y, min.y + _cellSize));
            return Vector2.Distance(point, nearest);
        }

        private int Index(Vector2Int cell) =>
            cell.x * _size.y + cell.y;

        private Vector2Int ToCell(Vector2 point) =>
            new(Mathf.FloorToInt(point.y / _cellSize), Mathf.FloorToInt(point.x / _cellSize));
    }
}
