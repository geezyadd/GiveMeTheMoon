using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // The routes of one field as the beacon layout reads them: the lanes that get beacons (open main and alternative
    // routes; the main route with the shortest centerline first, then the other main routes, then the alternatives)
    // and the corridor of every route, dead ends and closed alternatives too, which no beacon may enter. A
    // closed alternative is told by a closed cell on its centerline: the ridge that closes it.
    public sealed class FieldBeaconLanes {
        // One bit per lane; the last bit marks the corridors of the routes that are no lane.
        private const int MAX_LANES = 63;
        private const ulong NO_LANE_BIT = 1UL << MAX_LANES;

        private readonly List<FieldBeaconRoute> _lanes = new();
        private readonly Dictionary<Vector2Int, ulong> _laneMaskByCell = new();
        // A corridor is the squares of its half width around its centerline cells.
        private readonly Dictionary<Vector2Int, ulong> _corridorMaskByCell = new();
        private int _joinCells;

        public IReadOnlyList<FieldBeaconRoute> Lanes => _lanes;

        public void Read(IReadOnlyList<FieldBeaconRoute> routes, IFieldDebrisCells cells, int joinCells) {
            _lanes.Clear();
            _laneMaskByCell.Clear();
            _corridorMaskByCell.Clear();
            _joinCells = joinCells;
            SelectLanes(routes, cells);
            for (int i = 0; i < _lanes.Count; i++)
                AddLaneCells(_lanes[i], 1UL << i);

            foreach (FieldBeaconRoute route in routes)
                AddCorridor(route, LaneBit(route));
        }

        // Lanes of the mask other than the lane itself within the join distance of the cell.
        public bool IsNearLane(Vector2Int cell, ulong laneMask) {
            for (int dx = -_joinCells; dx <= _joinCells; dx++) {
                for (int dy = -_joinCells; dy <= _joinCells; dy++) {
                    if (_laneMaskByCell.TryGetValue(new Vector2Int(cell.x + dx, cell.y + dy), out ulong mask) && (mask & laneMask) != 0)
                        return true;
                }
            }

            return false;
        }

        // A point (cells, x along the field, y across) whose square of the clearance on each side touches no corridor
        // cell of any route.
        public bool IsClearOfRoutes(Vector2 point, float clearanceCells) {
            int maxX = Mathf.FloorToInt(point.x + clearanceCells);
            int maxY = Mathf.FloorToInt(point.y + clearanceCells);
            for (int x = Mathf.FloorToInt(point.x - clearanceCells); x <= maxX; x++) {
                for (int y = Mathf.FloorToInt(point.y - clearanceCells); y <= maxY; y++) {
                    if (_corridorMaskByCell.ContainsKey(new Vector2Int(x, y)))
                        return false;
                }
            }

            return true;
        }

        // A point inside the corridor of a route other than the lane (another lane, a dead end or a closed
        // alternative) and not in the lane's own corridor, which it may share with another lane.
        public bool IsInOtherCorridor(Vector2 point, int laneIndex) =>
            _corridorMaskByCell.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)), out ulong mask) && (mask & (1UL << laneIndex)) == 0;

        private ulong LaneBit(FieldBeaconRoute route) {
            int index = _lanes.IndexOf(route);
            return index >= 0 ? 1UL << index : NO_LANE_BIT;
        }

        private void AddCorridor(FieldBeaconRoute route, ulong bit) {
            for (int i = 0; i < route.Centerline.Count; i++)
                AddCorridorSquare(route.Centerline[i], route.HalfWidths[i], bit);
        }

        private void AddCorridorSquare(Vector2Int center, int halfWidth, ulong bit) {
            for (int x = center.x - halfWidth; x <= center.x + halfWidth; x++) {
                for (int y = center.y - halfWidth; y <= center.y + halfWidth; y++)
                    AddCorridorCell(new Vector2Int(x, y), bit);
            }
        }

        private void AddCorridorCell(Vector2Int cell, ulong bit) =>
            _corridorMaskByCell[cell] = _corridorMaskByCell.TryGetValue(cell, out ulong mask) ? mask | bit : bit;

        private void SelectLanes(IReadOnlyList<FieldBeaconRoute> routes, IFieldDebrisCells cells) {
            List<FieldBeaconRoute> mains = new();
            List<FieldBeaconRoute> alternatives = new();
            foreach (FieldBeaconRoute route in routes) {
                if (route.Centerline.Count == 0 || IsClosed(route, cells))
                    continue;

                if (route.Kind == FieldRouteKind.Main)
                    mains.Add(route);
                else if (route.Kind == FieldRouteKind.Alternative)
                    alternatives.Add(route);
            }

            mains.Sort(CompareMains);
            alternatives.Sort(CompareIds);
            _lanes.AddRange(mains);
            _lanes.AddRange(alternatives);
            if (_lanes.Count > MAX_LANES)
                throw new InvalidOperationException($"A field has {_lanes.Count} lanes; beacons support at most {MAX_LANES}.");
        }

        private void AddLaneCells(FieldBeaconRoute lane, ulong bit) {
            foreach (Vector2Int cell in lane.Centerline)
                _laneMaskByCell[cell] = _laneMaskByCell.TryGetValue(cell, out ulong mask) ? mask | bit : bit;
        }

        private static bool IsClosed(FieldBeaconRoute route, IFieldDebrisCells cells) {
            foreach (Vector2Int cell in route.Centerline) {
                if (cells.IsBlocked(cell))
                    return true;
            }

            return false;
        }

        private static int CompareMains(FieldBeaconRoute first, FieldBeaconRoute second) {
            int byLength = first.Centerline.Count.CompareTo(second.Centerline.Count);
            return byLength != 0 ? byLength : CompareIds(first, second);
        }

        private static int CompareIds(FieldBeaconRoute first, FieldBeaconRoute second) =>
            first.Id.CompareTo(second.Id);
    }
}
