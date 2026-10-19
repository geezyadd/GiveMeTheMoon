using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // Where the beacons of a field stand, in priority order, so a later one gives way to a kept one closer than the
    // least gap: first the entrances of the main routes and the forks and merges of all lanes (both sides), then the
    // sharp turns (outer side first), then one beacon every spacing along each lane (sides alternating). A stretch of
    // a lane that runs together with an earlier lane (channels near A and B, an alternative where it leaves and
    // rejoins) gets no turn, spacing or entrance beacons of its own. Dead ends and closed alternatives get none at all,
    // and no beacon enters their corridor either. Touches no Unity object: it runs on any thread.
    public sealed class FieldBeaconLayout : IFieldBeaconLayout {
        private const int RIGHT = 1;
        private const int LEFT = -1;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldBeaconLayout(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public void Fill(IReadOnlyList<FieldBeaconRoute> routes, float cellSize, IFieldDebrisCells cells, List<FieldBeacon> beacons) {
            beacons.Clear();
            FieldBeaconConfiguration configuration = _flightFieldConfiguration.Beacons;
            FieldBeaconLanes lanes = new();
            lanes.Read(routes, cells, configuration.LaneJoinCells);
            FieldBeaconPlacer placer = new(configuration, lanes, cells, cellSize, beacons);
            List<bool> isShared = new();
            for (int lane = 0; lane < lanes.Lanes.Count; lane++)
                AddEntranceAndForks(lanes, placer, lane, isShared);

            for (int lane = 0; lane < lanes.Lanes.Count; lane++)
                AddTurns(lanes, placer, lane, isShared);

            for (int lane = 0; lane < lanes.Lanes.Count; lane++)
                AddSpacing(lanes, placer, lane, cellSize, isShared);
        }

        private void AddEntranceAndForks(FieldBeaconLanes lanes, FieldBeaconPlacer placer, int lane, List<bool> isShared) {
            FieldBeaconRoute route = lanes.Lanes[lane];
            FillShared(lanes, lane, isShared);
            int entrance = Mathf.Min(_flightFieldConfiguration.Beacons.EntranceOffsetCells, route.Centerline.Count - 1);
            if (route.Kind == FieldRouteKind.Main && !isShared[entrance])
                placer.TryAddBothSides(lane, entrance, FieldBeaconReason.Entrance);

            ulong others = OtherLanes(lanes, lane);
            bool wasNear = lanes.IsNearLane(route.Centerline[0], others);
            for (int i = 1; i < route.Centerline.Count; i++) {
                bool isNear = lanes.IsNearLane(route.Centerline[i], others);
                if (isNear == wasNear)
                    continue;

                // The beacons stand where the lane is already on its own: after a fork, before a merge.
                placer.TryAddBothSides(lane, isNear ? i - 1 : i, FieldBeaconReason.Fork);
                wasNear = isNear;
            }
        }

        private void AddTurns(FieldBeaconLanes lanes, FieldBeaconPlacer placer, int lane, List<bool> isShared) {
            FieldBeaconRoute route = lanes.Lanes[lane];
            FillShared(lanes, lane, isShared);
            int window = _flightFieldConfiguration.Beacons.TurnWindowCells;
            for (int i = window; i < route.Centerline.Count - window; i++) {
                float turn = FieldBeaconHeading.TurnAt(route.Centerline, i, window);
                bool isSharpTurn = Mathf.Abs(turn) > _flightFieldConfiguration.Beacons.TurnAngleDegrees && IsSharpestAround(route.Centerline, i, window, Mathf.Abs(turn));
                if (!isSharpTurn || isShared[i])
                    continue;

                placer.TryAddEitherSide(lane, i, turn > 0f ? LEFT : RIGHT, FieldBeaconReason.Turn);
            }
        }

        private void AddSpacing(FieldBeaconLanes lanes, FieldBeaconPlacer placer, int lane, float cellSize, List<bool> isShared) {
            FieldBeaconRoute route = lanes.Lanes[lane];
            FillShared(lanes, lane, isShared);
            float spacing = _flightFieldConfiguration.Beacons.BeaconSpacingMeters;
            float travelled = 0f;
            int side = RIGHT;
            for (int i = 1; i < route.Centerline.Count; i++) {
                if (isShared[i]) {
                    travelled = 0f;
                    continue;
                }

                travelled += Vector2Int.Distance(route.Centerline[i - 1], route.Centerline[i]) * cellSize;
                if (travelled < spacing)
                    continue;

                travelled -= spacing;
                placer.TryAddEitherSide(lane, i, side, FieldBeaconReason.Spacing);
                side = -side;
            }
        }

        // The first of equal turns in a row counts, so a plateau gets one beacon.
        private static bool IsSharpestAround(IReadOnlyList<Vector2Int> centerline, int index, int window, float turn) {
            for (int i = index - window; i <= index + window; i++) {
                if (i == index)
                    continue;

                float other = Mathf.Abs(FieldBeaconHeading.TurnAt(centerline, i, window));
                bool isOutdone = i < index ? other >= turn : other > turn;
                if (isOutdone)
                    return false;
            }

            return true;
        }

        // A centerline cell runs together with an earlier lane when that lane is within the join distance.
        private static void FillShared(FieldBeaconLanes lanes, int lane, List<bool> isShared) {
            isShared.Clear();
            ulong earlier = (1UL << lane) - 1UL;
            foreach (Vector2Int cell in lanes.Lanes[lane].Centerline)
                isShared.Add(lanes.IsNearLane(cell, earlier));
        }

        private static ulong OtherLanes(FieldBeaconLanes lanes, int lane) {
            ulong all = (1UL << lanes.Lanes.Count) - 1UL;
            return all & ~(1UL << lane);
        }
    }
}
