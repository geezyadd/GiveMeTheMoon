using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // A fine grid over the field plane where a point is shut when an obstacle surface is closer than the clearance:
    // every closed cell (and the outside of the matrix) grown by the clearance, every rock in the
    // plane grown by the clearance. Stamped once, so the search looks up a flag instead of measuring distances.
    public sealed class FieldSlalomGrid {
        private const float CELL_CENTER = 0.5f;

        private readonly bool[] _isShut;

        public Vector2Int Size { get; }
        public float Step { get; }

        public FieldSlalomGrid(FieldSlalomObstacles obstacles, float step, float clearance) {
            Step = step;
            Size = new Vector2Int(Mathf.CeilToInt(obstacles.Size.y * obstacles.CellSize / step), Mathf.CeilToInt(obstacles.Size.x * obstacles.CellSize / step));
            _isShut = new bool[Size.x * Size.y];
            StampWalls(obstacles, clearance);
            foreach (FieldSlalomRock rock in obstacles.PlaneRocks)
                StampDisc(rock.Position, rock.Radius + clearance);
        }

        public bool IsShut(Vector2Int point) =>
            _isShut[Index(point)];

        public bool IsInside(Vector2Int point) =>
            point.x >= 0 && point.x < Size.x && point.y >= 0 && point.y < Size.y;

        public int Index(Vector2Int point) =>
            point.y * Size.x + point.x;

        public Vector2Int ToPoint(Vector2 plane) =>
            new(Mathf.Clamp(Mathf.FloorToInt(plane.x / Step), 0, Size.x - 1), Mathf.Clamp(Mathf.FloorToInt(plane.y / Step), 0, Size.y - 1));

        public Vector2 ToPlane(Vector2Int point) =>
            (point + Vector2.one * CELL_CENTER) * Step;

        // The ring of cells just outside the matrix counts as closed, so the field edge is a wall too.
        private void StampWalls(FieldSlalomObstacles obstacles, float clearance) {
            for (int along = -1; along <= obstacles.Size.x; along++) {
                for (int across = -1; across <= obstacles.Size.y; across++) {
                    Vector2Int cell = new(along, across);
                    if (obstacles.IsClosed(cell))
                        StampCell(obstacles, cell, IsSurrounded(obstacles, cell) ? 0f : clearance);
                }
            }
        }

        private static bool IsSurrounded(FieldSlalomObstacles obstacles, Vector2Int cell) {
            for (int along = -1; along <= 1; along++) {
                for (int across = -1; across <= 1; across++) {
                    if (!obstacles.IsClosed(cell + new Vector2Int(along, across)))
                        return false;
                }
            }

            return true;
        }

        // Field plane: x across (matrix y), y along (matrix x). Plain floats: this runs for millions of points.
        private void StampCell(FieldSlalomObstacles obstacles, Vector2Int cell, float reach) {
            float minX = cell.y * obstacles.CellSize;
            float minY = cell.x * obstacles.CellSize;
            float maxX = minX + obstacles.CellSize;
            float maxY = minY + obstacles.CellSize;
            float reachSquared = reach * reach;
            int firstX = Mathf.Max(0, Mathf.FloorToInt((minX - reach) / Step));
            int lastX = Mathf.Min(Size.x - 1, Mathf.FloorToInt((maxX + reach) / Step));
            int firstY = Mathf.Max(0, Mathf.FloorToInt((minY - reach) / Step));
            int lastY = Mathf.Min(Size.y - 1, Mathf.FloorToInt((maxY + reach) / Step));
            for (int y = firstY; y <= lastY; y++) {
                float planeY = (y + CELL_CENTER) * Step;
                float outY = Mathf.Max(0f, Mathf.Max(minY - planeY, planeY - maxY));
                int row = y * Size.x;
                for (int x = firstX; x <= lastX; x++) {
                    float planeX = (x + CELL_CENTER) * Step;
                    float outX = Mathf.Max(0f, Mathf.Max(minX - planeX, planeX - maxX));
                    float distanceSquared = outX * outX + outY * outY;
                    if (distanceSquared < reachSquared || distanceSquared == 0f)
                        _isShut[row + x] = true;
                }
            }
        }

        private void StampDisc(Vector2 center, float radius) {
            Vector2Int first = ToPointUnclamped(center - Vector2.one * radius);
            Vector2Int last = ToPointUnclamped(center + Vector2.one * radius);
            for (int y = Mathf.Max(0, first.y); y <= Mathf.Min(Size.y - 1, last.y); y++) {
                for (int x = Mathf.Max(0, first.x); x <= Mathf.Min(Size.x - 1, last.x); x++) {
                    Vector2Int point = new(x, y);
                    if (Vector2.Distance(ToPlane(point), center) < radius)
                        _isShut[Index(point)] = true;
                }
            }
        }

        private Vector2Int ToPointUnclamped(Vector2 plane) =>
            new(Mathf.FloorToInt(plane.x / Step), Mathf.FloorToInt(plane.y / Step));
    }
}
