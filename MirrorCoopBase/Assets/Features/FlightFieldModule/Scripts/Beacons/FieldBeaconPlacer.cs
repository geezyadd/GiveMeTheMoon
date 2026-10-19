using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // Puts the beacons of one field on the walls: from a centerline point it walks sideways out of the lane, past the
    // corridor's half width there and every passable (debris) cell, to the first closed or empty cell, and sets the
    // beacon a little inside it, outside the corridor of every route with the clearance. Where lanes run side by side
    // in one wide channel (Braid) a lane may have no wall of its own: then the beacon stands in the debris between the
    // corridors, at the first point clear of all of them. The walk stops at the corridor of any other route, so a
    // beacon never stands beyond another lane or in a dead end. A side with neither gets no beacon, nor does a point
    // closer than the least gap to a beacon already kept.
    public sealed class FieldBeaconPlacer {
        private const float CELL_CENTER = 0.5f;

        private readonly FieldBeaconConfiguration _fieldBeaconConfiguration;
        private readonly FieldBeaconLanes _fieldBeaconLanes;
        private readonly IFieldDebrisCells _cells;
        private readonly float _cellSize;
        private readonly List<FieldBeacon> _beacons;

        public FieldBeaconPlacer(FieldBeaconConfiguration fieldBeaconConfiguration, FieldBeaconLanes fieldBeaconLanes, IFieldDebrisCells cells, float cellSize,
                                 List<FieldBeacon> beacons) {
            _fieldBeaconConfiguration = fieldBeaconConfiguration;
            _fieldBeaconLanes = fieldBeaconLanes;
            _cells = cells;
            _cellSize = cellSize;
            _beacons = beacons;
        }

        // The side is +1 to the right of the lane's heading, -1 to the left.
        public bool TryAdd(int laneIndex, int centerlineIndex, int side, FieldBeaconReason reason) {
            FieldBeaconRoute lane = _fieldBeaconLanes.Lanes[laneIndex];
            Vector2Int cell = lane.Centerline[centerlineIndex];
            Vector2 center = new(cell.x + CELL_CENTER, cell.y + CELL_CENTER);
            Vector2 normal = RightOf(FieldBeaconHeading.At(lane.Centerline, centerlineIndex, _fieldBeaconConfiguration.TurnWindowCells)) * side;
            if (!TryFindWall(laneIndex, center, normal, lane.HalfWidths[centerlineIndex], out Vector2 point) || IsNearKeptBeacon(point))
                return false;

            Vector3 position = new(point.y * _cellSize, _fieldBeaconConfiguration.HeightMeters, point.x * _cellSize);
            _beacons.Add(new FieldBeacon(position, laneIndex == 0 ? FieldBeaconLane.Main : FieldBeaconLane.Alternative, reason, lane.Id));
            return true;
        }

        public void TryAddBothSides(int laneIndex, int centerlineIndex, FieldBeaconReason reason) {
            TryAdd(laneIndex, centerlineIndex, 1, reason);
            TryAdd(laneIndex, centerlineIndex, -1, reason);
        }

        // Tries the side first, then the other one.
        public void TryAddEitherSide(int laneIndex, int centerlineIndex, int side, FieldBeaconReason reason) {
            if (!TryAdd(laneIndex, centerlineIndex, side, reason))
                TryAdd(laneIndex, centerlineIndex, -side, reason);
        }

        private bool TryFindWall(int laneIndex, Vector2 center, Vector2 normal, int halfWidthCells, out Vector2 point) {
            point = default;
            float step = _fieldBeaconConfiguration.EdgeSearchStepMeters / _cellSize;
            float inset = _fieldBeaconConfiguration.WallInsetMeters / _cellSize;
            float clearance = (_fieldBeaconConfiguration.LaneClearanceMeters + _fieldBeaconConfiguration.BeaconRadiusMeters) / _cellSize;
            float start = halfWidthCells + CELL_CENTER;
            float end = start + _fieldBeaconConfiguration.EdgeSearchCells;
            bool hasFallback = false;
            for (float distance = start; distance <= end; distance += step) {
                Vector2 sample = center + normal * distance;
                if (_fieldBeaconLanes.IsInOtherCorridor(sample, laneIndex))
                    break;

                if (IsPassable(sample)) {
                    bool isFallback = !hasFallback && _fieldBeaconLanes.IsClearOfRoutes(sample, clearance);
                    point = isFallback ? sample : point;
                    hasFallback |= isFallback;
                    continue;
                }

                Vector2 candidate = center + normal * (distance + inset);
                if (!_fieldBeaconLanes.IsClearOfRoutes(candidate, clearance))
                    continue;

                point = candidate;
                return true;
            }

            return hasFallback;
        }

        private bool IsPassable(Vector2 point) =>
            _cells.TryGetDebris(new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)), out _, out _);

        // Plane points in cells: x along the field, y across.
        private bool IsNearKeptBeacon(Vector2 point) {
            float gap = _fieldBeaconConfiguration.MinBeaconGapMeters / _cellSize;
            foreach (FieldBeacon beacon in _beacons) {
                Vector2 kept = new(beacon.Position.z / _cellSize, beacon.Position.x / _cellSize);
                if ((kept - point).sqrMagnitude < gap * gap)
                    return true;
            }

            return false;
        }

        // Matrix x runs along Forward, y along Right: the right of heading (1, 0) is (0, 1).
        private static Vector2 RightOf(Vector2 heading) =>
            new(-heading.y, heading.x);
    }
}
