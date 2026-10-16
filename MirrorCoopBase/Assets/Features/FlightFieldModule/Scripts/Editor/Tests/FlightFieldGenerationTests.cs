using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Generation rules on 200 seeds at loops 0, 5 and 10 for every difficulty profile, with the game's configuration
    // and catalog. Every check rebuilds the occupied cells from the placement list, the way a participant builds the
    // field. The fields are generated in parallel (the generation touches no Unity object and keeps no state); the
    // generation time is measured on its own, one field after another. The balance table of every profile goes to
    // the log (metric failures, zone shares, L_free / L_route, dodges per minute, generation time).
    public sealed class FlightFieldGenerationTests {
        private const int SEED_COUNT = 200;
        private const int FIRST_SEED = 1;
        // Share of fields per profile and loop that may miss the "route matters" metric after the repair rounds.
        private const float MAX_METRIC_FAILURE_SHARE = 0.1f;
        private const float GAP_TOLERANCE = 0.001f;
        private const int TIMED_SEEDS = 5;

        private static readonly int[] _loops = { 0, 5, 10 };

        private readonly Dictionary<(string profile, int loop), List<FlightField>> _fields = new();
        private readonly Dictionary<(string profile, int loop), double> _milliseconds = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        private static IEnumerable<int> Loops => _loops;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                foreach (int loop in _loops)
                    GenerateAll(profile, loop);
            }

            LogBalanceTable();
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenTheMainRouteHasAPassageWithClearance(int loop) {
            int clearance = _composition.Configuration.PassageClearanceCells;
            foreach (FlightField field in FieldsOfLoop(loop)) {
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                foreach (FieldRoute route in field.Routes.Where(IsMain))
                    Assert.IsTrue(occupancy.HasPassage(route.Start, field.StationBCell, clearance), $"{Name(field)}: route {route.Id} has no passage to B.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenMainRoutesLeaveTheTakeoffCorridorAndEndAtB(int loop) {
            foreach (FlightField field in FieldsOfLoop(loop)) {
                foreach (FieldRoute route in field.Routes.Where(IsMain)) {
                    bool isAtCruiseStart = route.Start == field.CruiseStartCell || field.Cells[route.Start].Reserve == FieldReserveKind.TakeoffCorridor;
                    Assert.IsTrue(isAtCruiseStart, $"{Name(field)}: route {route.Id} does not start in the takeoff corridor.");
                    Assert.AreEqual(field.StationBCell, route.End, $"{Name(field)}: route {route.Id} does not end at B.");
                }
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenStationZonesTakeoffAndMainCorridorsHaveNoPlacement(int loop) {
            foreach (FlightField field in FieldsOfLoop(loop)) {
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                for (int x = 0; x < occupancy.Size.x; x++) {
                    for (int y = 0; y < occupancy.Size.y; y++)
                        AssertNotInEmptyZone(field, occupancy, new Vector2Int(x, y));
                }
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenPlacementsDoNotOverlapAndStayInside(int loop) {
            foreach (FlightField field in FieldsOfLoop(loop)) {
                Assert.IsNotEmpty(field.Placements, $"{Name(field)}: the field is empty.");
                CreateOccupancy(field);
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenTheBorderWallHasNoHolesOutsideReservedCells(int loop) {
            foreach (FlightField field in FieldsOfLoop(loop)) {
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                int thickness = FieldTestRequests.FindProfile(_composition.Configuration, field.ProfileId).WallThicknessCells;
                for (int x = 0; x < occupancy.Size.x; x++) {
                    for (int y = 0; y < occupancy.Size.y; y++)
                        AssertWallCellClosed(field, occupancy, new Vector2Int(x, y), thickness);
                }
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenNoPassageWasForced(int loop) {
            foreach (FlightField field in FieldsOfLoop(loop))
                Assert.IsFalse(field.IsPassageForced, $"{Name(field)}: the passage was forced.");
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenTheRouteMattersOnAlmostEveryField(int loop) {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                List<FlightField> fields = _fields[(profile.Id, loop)];
                float failureShare = fields.Count(field => !field.Metrics.IsPathMetricMet) / (float)fields.Count;

                Assert.LessOrEqual(failureShare, MAX_METRIC_FAILURE_SHARE, $"{profile.Id} loop {loop}: {failureShare:P0} of the fields have a short way outside the corridors.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenZoneSharesStayNearTheProfile(int loop) {
            float tolerance = _composition.Configuration.ZoneShareTolerance;
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                List<FlightField> fields = _fields[(profile.Id, loop)];
                float blocked = fields.Average(field => field.Metrics.BlockedShare);
                float debris = fields.Average(field => field.Metrics.DebrisShare);
                float difficulty = fields[0].Difficulty;

                Assert.AreEqual(profile.BlockedShare.Evaluate(difficulty), blocked, tolerance, $"{profile.Id} loop {loop}: blocked share.");
                Assert.AreEqual(profile.DebrisShare.Evaluate(difficulty), debris, tolerance, $"{profile.Id} loop {loop}: debris share.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenDebrisInCorridorsLeavesTheShipGap(int loop) {
            float minGap = _composition.Configuration.DebrisMinGapMeters;
            foreach (FlightField field in FieldsOfLoop(loop))
                Assert.GreaterOrEqual(field.Metrics.MinCorridorGapMeters, minGap - GAP_TOLERANCE, $"{Name(field)}: two corridor rocks are too close.");
        }

        [Test]
        public void WhenSameSeedIsGeneratedTwice_ThenPlacementsAndDebrisAreEqual() {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                foreach (int loop in _loops) {
                    FlightField first = _fields[(profile.Id, loop)][0];
                    FlightField again = Generate(profile, FIRST_SEED, loop);

                    CollectionAssert.AreEqual(first.Placements, again.Placements, $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other placements.");
                    CollectionAssert.AreEqual(DebrisCells(first), DebrisCells(again), $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other debris.");
                }
            }
        }

        [Test]
        public void WhenSeedsDiffer_ThenPlacementsDiffer() {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                List<FlightField> fields = _fields[(profile.Id, _loops[0])];
                for (int i = 1; i < fields.Count; i++)
                    CollectionAssert.AreNotEqual(fields[i - 1].Placements, fields[i].Placements, $"{profile.Id}: seeds {fields[i - 1].Seed} and {fields[i].Seed} gave the same field.");
            }
        }

        private void GenerateAll(FieldDifficultyProfile profile, int loop) {
            List<FieldRequest> requests = Enumerable.Range(FIRST_SEED, SEED_COUNT).Select(seed => FieldTestRequests.WithProfile(CreateRequest(seed, loop), profile)).ToList();
            FlightField[] fields = new FlightField[SEED_COUNT];
            Parallel.For(0, SEED_COUNT, index => fields[index] = _composition.Generator.Generate(requests[index]));
            _fields[(profile.Id, loop)] = fields.ToList();
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < TIMED_SEEDS; i++)
                _composition.Generator.Generate(requests[i]);

            _milliseconds[(profile.Id, loop)] = stopwatch.Elapsed.TotalMilliseconds / TIMED_SEEDS;
        }

        private FlightField Generate(FieldDifficultyProfile profile, int seed, int loop) =>
            _composition.Generator.Generate(FieldTestRequests.WithProfile(CreateRequest(seed, loop), profile));

        private FieldRequest CreateRequest(int seed, int loop) =>
            _scenario.CreateRequest(_composition.RequestFactory, seed, loop);

        private IEnumerable<FlightField> FieldsOfLoop(int loop) =>
            _composition.Configuration.DifficultyProfiles.SelectMany(profile => _fields[(profile.Id, loop)]);

        private FieldTestOccupancy CreateOccupancy(FlightField field) =>
            new(field, _composition.Catalog, _composition.FootprintReader);

        private void LogBalanceTable() {
            List<string> lines = new() { "[FieldBalance] profile | loop | metric failures | blocked | debris | empty | L_free/L_route (no way) | straight blocked | dodges/min | ms/field" };
            foreach (KeyValuePair<(string profile, int loop), List<FlightField>> entry in _fields)
                lines.Add(FormatBalanceLine(entry.Key, entry.Value));

            Debug.Log(string.Join("\n", lines));
        }

        private string FormatBalanceLine((string profile, int loop) key, List<FlightField> fields) {
            List<FieldMetrics> metrics = fields.Select(field => field.Metrics).ToList();
            List<FieldMetrics> withFreeWay = metrics.Where(metric => metric.HasFreePath).ToList();
            string ratio = withFreeWay.Count > 0 ? withFreeWay.Average(metric => metric.FreeToRouteRatio).ToString("0.00") : "-";
            return $"[FieldBalance] {key.profile} | {key.loop} | {metrics.Count(metric => !metric.IsPathMetricMet)}/{metrics.Count} | {metrics.Average(metric => metric.BlockedShare):P0} "
                + $"| {metrics.Average(metric => metric.DebrisShare):P0} | {metrics.Average(metric => metric.EmptyShare):P0} | {ratio} ({metrics.Count - withFreeWay.Count}) "
                + $"| {metrics.Count(metric => metric.IsStraightBlocked)}/{metrics.Count} | {metrics.Average(metric => metric.DodgesPerMinute):0.0} | {_milliseconds[key]:0.0}";
        }

        private void AssertNotInEmptyZone(FlightField field, FieldTestOccupancy occupancy, Vector2Int cell) {
            if (!occupancy.IsOccupied(cell))
                return;

            FieldReserveKind reserve = field.Cells[cell].Reserve;
            bool isKeptEmpty = reserve is FieldReserveKind.StationZone or FieldReserveKind.TakeoffCorridor or FieldReserveKind.RouteCorridor or FieldReserveKind.DeadEndCorridor;
            if (isKeptEmpty)
                Assert.Fail($"{Name(field)}: structure at {cell} in a {reserve} cell.");
        }

        private static void AssertWallCellClosed(FlightField field, FieldTestOccupancy occupancy, Vector2Int cell, int thickness) {
            Vector2Int size = occupancy.Size;
            int toBorder = Mathf.Min(Mathf.Min(cell.x, size.x - 1 - cell.x), Mathf.Min(cell.y, size.y - 1 - cell.y));
            if (toBorder >= thickness)
                return;

            bool isClosed = occupancy.IsOccupied(cell) || field.Cells[cell].IsReserved;
            if (!isClosed)
                Assert.Fail($"{Name(field)}: hole in the wall at {cell}.");
        }

        private static List<(Vector2Int cell, float density, string id)> DebrisCells(FlightField field) {
            List<(Vector2Int, float, string)> cells = new();
            foreach (Vector2Int cell in field.Cells.GetIndexes(HasDebris))
                cells.Add((cell, field.Cells[cell].DebrisDensity, field.Cells[cell].DebrisId));

            return cells;
        }

        private static bool HasDebris(FieldCell cell) =>
            cell.HasDebris;

        private static bool IsMain(FieldRoute route) =>
            route.Kind == FieldRouteKind.Main;

        private static string Name(FlightField field) =>
            $"{field.ProfileId} seed {field.Seed}";
    }
}
