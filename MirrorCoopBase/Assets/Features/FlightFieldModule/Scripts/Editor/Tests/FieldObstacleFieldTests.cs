using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The obstacle field guarantees on 200 seeds at loops 0, 5 and 10 for every obstacle profile, with every rock laid
    // out as the peers build it: a ship of the configured width plus the gap margin finds a way from the cruise start
    // to B, the narrowest gap along it leaves the ship room (less the path finder's grid tolerance), every two rocks in
    // the flight plane (debris and giants) are at least the ship gap apart, and every debris rock crosses the plane.
    // The same seed gives the same field and rocks. The variant table goes to the log ([FieldObstacles]: ways found,
    // rocks per km2, sizes, narrowest gap on the way, dodges per minute, largest rock, giants).
    [Timeout(TEST_TIMEOUT_MILLISECONDS)]
    public sealed class FieldObstacleFieldTests {
        private const int TEST_TIMEOUT_MILLISECONDS = 3600000;
        private const int SEED_COUNT = 200;
        private const int FIRST_SEED = 1;
        // A step between two neighbouring grid points is not measured on its own: it may cut a rock's curve this much.
        private const float STEP_CUT_METERS = 0.25f;
        private const float GAP_TOLERANCE = 0.01f;

        private static readonly int[] _loops = { 0, 5, 10 };
        // The upper diameters of the small, medium and large rocks in the table; above them the massive ones.
        private static readonly float[] _sizeLimitsMeters = { 35f, 150f, 350f };

        private readonly Dictionary<(string profile, int loop), List<FieldObstacleSummary>> _results = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        private static IEnumerable<int> Loops => _loops;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            foreach (FieldDifficultyProfile profile in FieldTestProfiles.Obstacles(_composition.Configuration)) {
                foreach (int loop in _loops)
                    _results[(profile.Id, loop)] = SummarizeAll(profile, loop);
            }

            LogTable();
        }

        [Test]
        public void WhenTheConfigurationIsRead_ThenItHasTheFourObstacleVariants() =>
            CollectionAssert.AreEquivalent(new[] { "obstacles_sparse", "obstacles_medium", "obstacles_dense", "obstacles_giants" },
                FieldTestProfiles.Obstacles(_composition.Configuration).Select(profile => profile.Id));

        [TestCaseSource(nameof(Loops))]
        public void WhenObstacleFieldsAreGenerated_ThenAShipFindsAWayToB(int loop) {
            foreach ((string profile, FieldObstacleSummary summary) in ResultsOfLoop(loop)) {
                if (!summary.IsPathFound)
                    Assert.Fail($"{profile} seed {summary.Seed} loop {loop}: no way for the ship from the cruise start to B.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenObstacleFieldsAreGenerated_ThenTheNarrowestGapAlongTheWayLeavesTheShipRoom(int loop) {
            float minGap = _composition.Configuration.DebrisMinGapMeters - 2f * (FieldSlalomPathFinder.ToleranceMeters + STEP_CUT_METERS);
            foreach ((string profile, FieldObstacleSummary summary) in ResultsOfLoop(loop)) {
                if (summary.IsPathFound && summary.PathGapMeters < minGap)
                    Assert.Fail($"{profile} seed {summary.Seed} loop {loop}: the way passes a gap of {summary.PathGapMeters:0.0} m.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenObstacleFieldsAreGenerated_ThenEveryTwoPlaneRocksLeaveTheShipGap(int loop) {
            float minGap = _composition.Configuration.DebrisMinGapMeters - GAP_TOLERANCE;
            foreach ((string profile, FieldObstacleSummary summary) in ResultsOfLoop(loop)) {
                if (summary.PlaneGapMeters < minGap)
                    Assert.Fail($"{profile} seed {summary.Seed} loop {loop}: two obstacles in the flight plane are only {summary.PlaneGapMeters:0.0} m apart.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenObstacleFieldsAreGenerated_ThenEveryDebrisRockCrossesThePlane(int loop) {
            foreach ((string profile, FieldObstacleSummary summary) in ResultsOfLoop(loop)) {
                if (!summary.AreAllRocksInPlane)
                    Assert.Fail($"{profile} seed {summary.Seed} loop {loop}: a debris rock stands off the plane, inside the volume's clearance band.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenSameSeedIsGeneratedTwice_ThenGiantsDebrisAndRocksAreEqual(int loop) {
            foreach (FieldDifficultyProfile profile in FieldTestProfiles.Obstacles(_composition.Configuration)) {
                FlightField first = Generate(profile, FIRST_SEED, loop);
                FlightField again = Generate(profile, FIRST_SEED, loop);

                CollectionAssert.AreEqual(first.Placements, again.Placements, $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other giants.");
                CollectionAssert.AreEqual(DebrisCells(first), DebrisCells(again), $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other debris cells.");
                CollectionAssert.AreEqual(Analyze(first).Rocks, Analyze(again).Rocks, $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other rocks.");
            }
        }

        private List<FieldObstacleSummary> SummarizeAll(FieldDifficultyProfile profile, int loop) {
            FieldObstacleSummary[] summaries = new FieldObstacleSummary[SEED_COUNT];
            Parallel.For(0, SEED_COUNT, index => summaries[index] = Summarize(profile, FIRST_SEED + index, loop));
            return summaries.ToList();
        }

        // One analyzer and gap meter per call: their workspaces are not shared between threads.
        private FieldObstacleSummary Summarize(FieldDifficultyProfile profile, int seed, int loop) {
            FlightField field = Generate(profile, seed, loop);
            FieldSlalomReport report = Analyze(field);
            float planeGap = new FieldPlaneGapMeter().Measure(field, report.Rocks);
            bool areAllInPlane = report.Rocks.All(rock => rock.IsInPlane);
            return new FieldObstacleSummary(seed, report, planeGap, areAllInPlane, report.CountBySize(_sizeLimitsMeters), field.Metrics.DodgesPerMinute);
        }

        private FieldSlalomReport Analyze(FlightField field) =>
            new FieldSlalomAnalyzer(_composition).Analyze(field);

        private FlightField Generate(FieldDifficultyProfile profile, int seed, int loop) =>
            _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, loop), profile));

        private IEnumerable<(string profile, FieldObstacleSummary summary)> ResultsOfLoop(int loop) =>
            _results.Where(entry => entry.Key.loop == loop).SelectMany(entry => entry.Value.Select(summary => (entry.Key.profile, summary)));

        private static List<(Vector2Int cell, float density, string id)> DebrisCells(FlightField field) {
            List<(Vector2Int, float, string)> cells = new();
            foreach (Vector2Int cell in field.Cells.GetIndexes(HasDebris))
                cells.Add((cell, field.Cells[cell].DebrisDensity, field.Cells[cell].DebrisId));

            return cells;
        }

        private static bool HasDebris(FieldCell cell) =>
            cell.HasDebris;

        private void LogTable() {
            List<string> lines = new() {
                "[FieldObstacles] profile | loop | ways found | way / straight | rocks per km2 | sizes <=35 / <=150 / <=350 / above (per field) | narrowest gap on way min / avg, m "
                + "| narrowest plane gap, m | dodges/min | largest rock, m (max) | giants per field",
            };
            foreach (KeyValuePair<(string profile, int loop), List<FieldObstacleSummary>> entry in _results)
                lines.Add(FormatLine(entry.Key, entry.Value));

            Debug.Log(string.Join("\n", lines));
        }

        private static string FormatLine((string profile, int loop) key, List<FieldObstacleSummary> summaries) {
            List<FieldObstacleSummary> found = summaries.Where(summary => summary.IsPathFound).ToList();
            string ratio = found.Count > 0 ? found.Average(summary => summary.PathToStraightRatio).ToString("0.00") : "-";
            string gaps = found.Count > 0 ? $"{found.Min(summary => summary.PathGapMeters):0.0} / {found.Average(summary => summary.PathGapMeters):0.0}" : "-";
            string sizes = string.Join(" / ", Enumerable.Range(0, _sizeLimitsMeters.Length + 1).Select(index => summaries.Average(summary => summary.SizeCounts[index]).ToString("0")));
            return $"[FieldObstacles] {key.profile} | {key.loop} | {found.Count}/{summaries.Count} | {ratio} | {summaries.Average(summary => summary.RocksPerSquareKilometer):0} | {sizes} "
                + $"| {gaps} | {summaries.Min(summary => summary.PlaneGapMeters):0.0} | {summaries.Average(summary => summary.DodgesPerMinute):0.0} "
                + $"| {summaries.Average(summary => summary.LargestRockMeters):0} ({summaries.Max(summary => summary.LargestRockMeters):0}) | {summaries.Average(summary => summary.Giants):0.0}";
        }
    }
}
