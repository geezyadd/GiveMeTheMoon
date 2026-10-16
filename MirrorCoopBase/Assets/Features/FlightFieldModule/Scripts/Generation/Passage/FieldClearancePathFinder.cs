using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Passage {
    // Breadth-first search over side moves: a cell is passable when it and every cell within the passage clearance
    // (diagonals included) are inside the field and have no placement. Debris does not block: the ship flies around it.
    public sealed class FieldClearancePathFinder : IFieldClearancePathFinder {
        private static readonly Vector2Int[] _sideSteps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldClearancePathFinder(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public List<Vector2Int> Find(IMatrix<FieldCell> cells, Vector2Int start, Vector2Int end, bool avoidCorridors) {
            Vector2Int size = new(cells.GetColumnCount(), cells.GetRowCount());
            bool[,] isPassable = CreatePassable(cells, size, start, end, avoidCorridors);
            if (!isPassable[start.x, start.y] || !isPassable[end.x, end.y])
                return new List<Vector2Int>();

            Vector2Int[,] cameFrom = new Vector2Int[size.x, size.y];
            bool[,] isSeen = new bool[size.x, size.y];
            Queue<Vector2Int> open = new();
            open.Enqueue(start);
            isSeen[start.x, start.y] = true;
            while (open.Count > 0) {
                Vector2Int cell = open.Dequeue();
                if (cell == end)
                    return Trace(cameFrom, start, end);

                OpenNeighbours(cell, size, isPassable, isSeen, cameFrom, open);
            }

            return new List<Vector2Int>();
        }

        private static void OpenNeighbours(Vector2Int cell, Vector2Int size, bool[,] isPassable, bool[,] isSeen, Vector2Int[,] cameFrom, Queue<Vector2Int> open) {
            foreach (Vector2Int step in _sideSteps) {
                Vector2Int next = cell + step;
                bool isNew = next.x >= 0 && next.x < size.x && next.y >= 0 && next.y < size.y && isPassable[next.x, next.y] && !isSeen[next.x, next.y];
                if (!isNew)
                    continue;

                isSeen[next.x, next.y] = true;
                cameFrom[next.x, next.y] = cell;
                open.Enqueue(next);
            }
        }

        private static List<Vector2Int> Trace(Vector2Int[,] cameFrom, Vector2Int start, Vector2Int end) {
            List<Vector2Int> path = new() { end };
            for (Vector2Int cell = end; cell != start;) {
                cell = cameFrom[cell.x, cell.y];
                path.Add(cell);
            }

            path.Reverse();
            return path;
        }

        private bool[,] CreatePassable(IMatrix<FieldCell> cells, Vector2Int size, Vector2Int start, Vector2Int end, bool avoidCorridors) {
            int clearance = _flightFieldConfiguration.PassageClearanceCells;
            bool[,] isPassable = new bool[size.x, size.y];
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    Vector2Int cell = new(x, y);
                    bool isAllowed = !avoidCorridors || !cells[cell].IsCorridor || cell == start || cell == end;
                    isPassable[x, y] = isAllowed && HasClearance(cells, cell, clearance, size);
                }
            }

            return isPassable;
        }

        private static bool HasClearance(IMatrix<FieldCell> cells, Vector2Int cell, int clearance, Vector2Int size) {
            for (int y = cell.y - clearance; y <= cell.y + clearance; y++) {
                for (int x = cell.x - clearance; x <= cell.x + clearance; x++) {
                    bool isBlocked = x < 0 || x >= size.x || y < 0 || y >= size.y || cells[x, y].IsOccupied;
                    if (isBlocked)
                        return false;
                }
            }

            return true;
        }
    }
}
