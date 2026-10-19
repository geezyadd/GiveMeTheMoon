using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Beacons;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The beacons of generated fields of every profile (Channels stages included) at an early and a late loop, laid out
    // from the routes and cells as a peer sees them: the same every time, on every main lane, never inside the
    // corridor of any route (dead ends and closed alternatives included) nor in reach of the slalom way.
    // The beacon counts per profile go to the log.
    public sealed class FieldBeaconLayoutTests {
        private const int SEED_COUNT = 12;
        private const int SLALOM_SEED_COUNT = 6;
        private const int FIRST_SEED = 1;
        private const float CELL_CENTER = 0.5f;
        private const float HALF = 0.5f;
        private const string FORK_PROFILE = "channels_fork";
        private const string BRAID_PROFILE = "channels_braid";
        private const string CHANNEL_PROFILE_PREFIX = "channels_";
        private const float MAX_UNMARKED_BRAID_LANE_SHARE = 0.05f;

        private static readonly int[] _loops = { 1, 6 };

        private readonly FieldDebugScenario _scenario = new();
        private readonly List<LaidOutField> _fields = new();

        private FlightFieldEditorComposition _composition;
        private FieldBeaconLayout _layout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _layout = new FieldBeaconLayout(_composition.Configuration);
            List<(FieldDifficultyProfile profile, int seed, int loop)> cases = new();
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                foreach (int loop in _loops) {
                    for (int seed = FIRST_SEED; seed < FIRST_SEED + SEED_COUNT; seed++)
                        cases.Add((profile, seed, loop));
                }
            }

            LaidOutField[] fields = new LaidOutField[cases.Count];
            Parallel.For(0, cases.Count, index => fields[index] = LayOut(cases[index].profile, cases[index].seed, cases[index].loop));
            _fields.AddRange(fields);
            LogBeaconTable();
        }

        [Test]
        public void WhenAFieldIsLaidOutTwiceWithTheRoutesInAnotherOrder_ThenTheBeaconsAreEqual() {
            foreach (LaidOutField laidOut in _fields.Where(entry => entry.Seed == FIRST_SEED)) {
                List<FieldBeaconRoute> reversed = BeaconRoutes(laidOut.Field);
                reversed.Reverse();
                List<FieldBeacon> again = new();
                _layout.Fill(reversed, laidOut.Field.Frame.CellSize, Cells(laidOut.Field), again);

                CollectionAssert.AreEqual(laidOut.Beacons, again, $"{laidOut.Field.ProfileId} loop {laidOut.Loop}: other beacons on the second layout.");
            }
        }

        [Test]
        public void WhenFieldsAreLaidOut_ThenEveryMainLaneHasBeacons() {
            foreach (LaidOutField laidOut in _fields.Where(entry => entry.Field.ProfileId != BRAID_PROFILE)) {
                foreach (FieldRoute route in laidOut.Field.Routes.Where(route => route.Kind == FieldRouteKind.Main)) {
                    if (laidOut.Beacons.All(beacon => beacon.RouteId != route.Id))
                        Assert.Fail($"{Describe(laidOut)}: main route {route.Id} has no beacon.");
                }
            }
        }

        // On Braid the channels run together in one wide channel: a middle one squeezed between the others' corridors
        // all along has no wall or gap of its own, and the beacons of the outer channels frame the shared channel.
        [Test]
        public void WhenBraidFieldsAreLaidOut_ThenAlmostEveryMainLaneHasBeacons() {
            List<LaidOutField> braidFields = _fields.Where(entry => entry.Field.ProfileId == BRAID_PROFILE).ToList();
            int mainLanes = braidFields.Sum(entry => entry.Field.Routes.Count(route => route.Kind == FieldRouteKind.Main));
            int unmarked = braidFields.Sum(CountUnmarkedMainLanes);

            Assert.LessOrEqual(unmarked, mainLanes * MAX_UNMARKED_BRAID_LANE_SHARE, $"{unmarked} of {mainLanes} Braid main lanes have no beacon.");
        }

        [Test]
        public void WhenFieldsAreLaidOut_ThenNoBeaconMarksOrEntersADeadEndOrAClosedAlternative() {
            foreach (LaidOutField laidOut in _fields) {
                List<FieldRoute> falseWays = laidOut.Field.Routes.Where(IsFalseWay).ToList();
                foreach (FieldBeacon beacon in laidOut.Beacons) {
                    FieldRoute marked = laidOut.Field.Routes.First(route => route.Id == beacon.RouteId);
                    Assert.IsFalse(IsFalseWay(marked), $"{Describe(laidOut)}: a beacon marks route {marked.Id} ({marked.Kind}, closed {marked.IsClosed}).");
                    foreach (FieldRoute falseWay in falseWays)
                        AssertOutsideCorridor(laidOut, beacon, falseWay, 0f);
                }
            }
        }

        [Test]
        public void WhenFieldsAreLaidOut_ThenNoBeaconEntersTheCorridorOfALane() {
            float radiusCells = _composition.Configuration.Beacons.BeaconRadiusMeters / _composition.Configuration.CellSize;
            foreach (LaidOutField laidOut in _fields) {
                foreach (FieldBeacon beacon in laidOut.Beacons) {
                    foreach (FieldRoute route in laidOut.Field.Routes.Where(route => !IsFalseWay(route)))
                        AssertOutsideCorridor(laidOut, beacon, route, radiusCells);
                }
            }
        }

        [Test]
        public void WhenAnAlternativeIsClosed_ThenThePeerReadsItAsNoLane() {
            foreach (LaidOutField laidOut in _fields) {
                FieldBeaconLanes lanes = new();
                lanes.Read(BeaconRoutes(laidOut.Field), Cells(laidOut.Field), _composition.Configuration.Beacons.LaneJoinCells);
                List<int> expected = laidOut.Field.Routes.Where(route => !IsFalseWay(route)).Select(route => route.Id).OrderBy(id => id).ToList();

                CollectionAssert.AreEqual(expected, lanes.Lanes.Select(lane => lane.Id).OrderBy(id => id).ToList(), $"{Describe(laidOut)}: other lanes than the open routes.");
            }
        }

        [Test]
        public void WhenBraidFieldsAreLaidOut_ThenTheCrossingsOfSeveralLanesGetForkBeacons() {
            foreach (LaidOutField laidOut in _fields.Where(entry => entry.Field.ProfileId == BRAID_PROFILE)) {
                int lanesWithForks = laidOut.Beacons.Where(beacon => beacon.Reason == FieldBeaconReason.Fork).Select(beacon => beacon.RouteId).Distinct().Count();

                Assert.GreaterOrEqual(lanesWithForks, 2, $"{Describe(laidOut)}: fork beacons on {lanesWithForks} lanes.");
            }
        }

        [Test]
        public void WhenForkAndBraidFieldsAreLaidOut_ThenBothColoursShow() {
            foreach (LaidOutField laidOut in _fields.Where(entry => entry.Field.ProfileId is FORK_PROFILE or BRAID_PROFILE)) {
                Assert.IsTrue(laidOut.Beacons.Any(beacon => beacon.Lane == FieldBeaconLane.Main), $"{Describe(laidOut)}: no main beacon.");
                Assert.IsTrue(laidOut.Beacons.Any(beacon => beacon.Lane == FieldBeaconLane.Alternative), $"{Describe(laidOut)}: no beacon of the other channels.");
            }
        }

        [Test]
        public void WhenChannelFieldsAreLaidOut_ThenNoBeaconComesIntoTheSlalomWaysClearance() {
            float clearance = _composition.Configuration.DebrisMinGapMeters * HALF + _composition.Configuration.Beacons.BeaconRadiusMeters;
            List<LaidOutField> channelFields = _fields.Where(entry => IsChannelProfile(entry.Field.ProfileId) && entry.Seed < FIRST_SEED + SLALOM_SEED_COUNT).ToList();
            FieldSlalomReport[] reports = new FieldSlalomReport[channelFields.Count];
            Parallel.For(0, channelFields.Count, index => reports[index] = new FieldSlalomAnalyzer(_composition).Analyze(channelFields[index].Field));
            for (int i = 0; i < channelFields.Count; i++) {
                foreach (FieldBeacon beacon in channelFields[i].Beacons) {
                    float distance = DistanceToPath(new Vector2(beacon.Position.x, beacon.Position.z), reports[i].Path);
                    if (distance < clearance)
                        Assert.Fail($"{Describe(channelFields[i])}: a beacon stands {distance:0.0} m from the slalom way.");
                }
            }
        }

        private LaidOutField LayOut(FieldDifficultyProfile profile, int seed, int loop) {
            FlightField field = _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, loop), profile));
            List<FieldBeacon> beacons = new();
            _layout.Fill(BeaconRoutes(field), field.Frame.CellSize, Cells(field), beacons);
            return new LaidOutField(field, seed, loop, beacons);
        }

        private MatrixFieldDebrisCells Cells(FlightField field) =>
            new(field.Cells, _composition.Catalog);

        // The routes as a peer gets them: kind, centerline and half widths, no closed flag.
        private static List<FieldBeaconRoute> BeaconRoutes(FlightField field) =>
            field.Routes.Select(route => new FieldBeaconRoute(route.Id, route.Kind, route.Centerline, route.HalfWidths)).ToList();

        // A corridor is the squares of its half width around its centerline cells.
        private static void AssertOutsideCorridor(LaidOutField laidOut, FieldBeacon beacon, FieldRoute route, float marginCells) {
            float cellSize = laidOut.Field.Frame.CellSize;
            Vector2 point = new(beacon.Position.z / cellSize, beacon.Position.x / cellSize);
            for (int i = 0; i < route.Centerline.Count; i++) {
                Vector2Int cell = route.Centerline[i];
                float reach = route.HalfWidths[i] + CELL_CENTER + marginCells;
                bool isInside = Mathf.Abs(point.x - cell.x - CELL_CENTER) < reach && Mathf.Abs(point.y - cell.y - CELL_CENTER) < reach;
                if (isInside)
                    Assert.Fail($"{Describe(laidOut)}: a beacon ({beacon.Reason}, route {beacon.RouteId}) stands inside route {route.Id} ({route.Kind}) at centerline cell {cell}.");
            }
        }

        private static bool IsFalseWay(FieldRoute route) =>
            route.Kind == FieldRouteKind.DeadEnd || route.IsClosed;

        private static bool IsChannelProfile(string profileId) =>
            profileId.StartsWith(CHANNEL_PROFILE_PREFIX);

        private static float DistanceToPath(Vector2 point, IReadOnlyList<Vector2> path) {
            float distance = float.MaxValue;
            for (int i = 1; i < path.Count; i++)
                distance = Mathf.Min(distance, DistanceToSegment(point, path[i - 1], path[i]));

            return distance;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to) {
            Vector2 segment = to - from;
            float along = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude) : 0f;
            return Vector2.Distance(point, from + segment * along);
        }

        private static string Describe(LaidOutField laidOut) =>
            $"{laidOut.Field.ProfileId} seed {laidOut.Seed} loop {laidOut.Loop}";

        private void LogBeaconTable() {
            List<string> lines = new() { "[FieldBeacons] profile | loop | beacons avg / max | main | other | entrance | fork | turn | spacing | main lanes without a beacon" };
            foreach (IGrouping<(string, int), LaidOutField> group in _fields.GroupBy(entry => (entry.Field.ProfileId, entry.Loop)))
                lines.Add(FormatLine(group.Key, group.ToList()));

            Debug.Log(string.Join("\n", lines));
        }

        private static string FormatLine((string profile, int loop) key, List<LaidOutField> fields) {
            List<FieldBeacon> all = fields.SelectMany(entry => entry.Beacons).ToList();
            float count = fields.Count;
            return $"[FieldBeacons] {key.profile} | {key.loop} | {all.Count / count:0} / {fields.Max(entry => entry.Beacons.Count)} | {all.Count(beacon => beacon.Lane == FieldBeaconLane.Main) / count:0} "
                + $"| {all.Count(beacon => beacon.Lane == FieldBeaconLane.Alternative) / count:0} | {Average(all, FieldBeaconReason.Entrance, count)} | {Average(all, FieldBeaconReason.Fork, count)} "
                + $"| {Average(all, FieldBeaconReason.Turn, count)} | {Average(all, FieldBeaconReason.Spacing, count)} | {fields.Sum(CountUnmarkedMainLanes)}";
        }

        private static int CountUnmarkedMainLanes(LaidOutField laidOut) =>
            laidOut.Field.Routes.Count(route => route.Kind == FieldRouteKind.Main && laidOut.Beacons.All(beacon => beacon.RouteId != route.Id));

        private static string Average(List<FieldBeacon> beacons, FieldBeaconReason reason, float fieldCount) =>
            (beacons.Count(beacon => beacon.Reason == reason) / fieldCount).ToString("0.0");
    }
}
