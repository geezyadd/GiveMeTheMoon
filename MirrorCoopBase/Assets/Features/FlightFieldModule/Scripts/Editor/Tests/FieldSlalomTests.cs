using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The slalom guarantee on 200 seeds at loops 0, 1, 5 and 10, for every channel profile the loop table offers there:
    // with every debris rock laid out as the peers build it, a ship of the configured width plus the gap margin finds
    // a way from the cruise start to B, and the narrowest gap along that way is at least the ship width plus margin,
    // less the path finder's grid tolerance on each side.
    // The slalom table of every profile goes to the log (ways found, way / straight line, narrowest gap, rocks per km
    // of main channel, dodges per minute).
    public sealed class FieldSlalomTests {
        private const int SEED_COUNT = 200;
        private const int FIRST_SEED = 1;
        // A step between two neighbouring grid points is not measured on its own: it may cut a rock's curve this much.
        private const float STEP_CUT_METERS = 0.25f;

        private static readonly int[] _loops = { 0, 1, 5, 10 };

        private readonly Dictionary<(string profile, int loop), List<(FlightField field, FieldSlalomReport report)>> _results = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        private static IEnumerable<int> Loops => _loops;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            foreach (int loop in _loops) {
                foreach (FieldDifficultyProfile profile in ProfilesOfLoop(loop))
                    _results[(profile.Id, loop)] = AnalyzeAll(profile, loop);
            }

            LogSlalomTable();
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenAShipFindsASlalomWayToB(int loop) {
            foreach ((FlightField field, FieldSlalomReport report) in ResultsOfLoop(loop)) {
                if (!report.IsPathFound)
                    Assert.Fail($"{field.ProfileId} seed {field.Seed} loop {loop}: no way for the ship from the cruise start to B.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenFieldsAreGenerated_ThenTheNarrowestGapAlongTheWayLeavesTheShipRoom(int loop) {
            float minGap = _composition.Configuration.DebrisMinGapMeters - 2f * (FieldSlalomPathFinder.ToleranceMeters + STEP_CUT_METERS);
            foreach ((FlightField field, FieldSlalomReport report) in ResultsOfLoop(loop)) {
                if (report.IsPathFound && report.MinGapMeters < minGap)
                    Assert.Fail($"{field.ProfileId} seed {field.Seed} loop {loop}: the way passes a gap of {report.MinGapMeters:0.0} m.");
            }
        }

        [TestCaseSource(nameof(Loops))]
        public void WhenSameSeedIsAnalyzedTwice_ThenTheRocksAreEqual(int loop) {
            FieldSlalomAnalyzer analyzer = new(_composition);
            foreach (FieldDifficultyProfile profile in ProfilesOfLoop(loop)) {
                FieldSlalomReport first = _results[(profile.Id, loop)][0].report;
                FieldSlalomReport again = analyzer.Analyze(Generate(profile, FIRST_SEED, loop));

                CollectionAssert.AreEqual(first.Rocks, again.Rocks, $"{profile.Id} loop {loop}: seed {FIRST_SEED} gave other rocks.");
            }
        }

        private List<(FlightField, FieldSlalomReport)> AnalyzeAll(FieldDifficultyProfile profile, int loop) {
            (FlightField, FieldSlalomReport)[] results = new (FlightField, FieldSlalomReport)[SEED_COUNT];
            Parallel.For(0, SEED_COUNT, index => results[index] = Analyze(profile, FIRST_SEED + index, loop));
            return results.ToList();
        }

        // One analyzer per call: its debris workspace is not shared between threads.
        private (FlightField, FieldSlalomReport) Analyze(FieldDifficultyProfile profile, int seed, int loop) {
            FlightField field = Generate(profile, seed, loop);
            return (field, new FieldSlalomAnalyzer(_composition).Analyze(field));
        }

        private FlightField Generate(FieldDifficultyProfile profile, int seed, int loop) =>
            _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, loop), profile));

        private IEnumerable<FieldDifficultyProfile> ProfilesOfLoop(int loop) {
            FlightFieldConfiguration.LoopRoutes routes = _composition.Configuration.RoutesByLoop.Where(entry => entry.FromLoop <= loop).OrderBy(entry => entry.FromLoop).Last();
            return routes.Profiles.Select(option => option.Profile).Where(IsChannelProfile);
        }

        // A channel profile closes every cell outside its corridors.
        private static bool IsChannelProfile(FieldDifficultyProfile profile) =>
            profile.BlockedShare.Evaluate(0f) >= 1f;

        private IEnumerable<(FlightField field, FieldSlalomReport report)> ResultsOfLoop(int loop) =>
            _results.Where(entry => entry.Key.loop == loop).SelectMany(entry => entry.Value);

        private void LogSlalomTable() {
            List<string> lines = new() { "[FieldSlalom] profile | loop | ways found | way / straight | narrowest gap min / avg, m | rocks per km of channel | dodges/min" };
            foreach (KeyValuePair<(string profile, int loop), List<(FlightField field, FieldSlalomReport report)>> entry in _results)
                lines.Add(FormatLine(entry.Key, entry.Value));

            Debug.Log(string.Join("\n", lines));
        }

        private static string FormatLine((string profile, int loop) key, List<(FlightField field, FieldSlalomReport report)> results) {
            List<FieldSlalomReport> found = results.Select(result => result.report).Where(report => report.IsPathFound).ToList();
            string ratio = found.Count > 0 ? found.Average(report => report.PathToStraightRatio).ToString("0.00") : "-";
            string gaps = found.Count > 0 ? $"{found.Min(report => report.MinGapMeters):0.0} / {found.Average(report => report.MinGapMeters):0.0}" : "-";
            return $"[FieldSlalom] {key.profile} | {key.loop} | {found.Count}/{results.Count} | {ratio} | {gaps} | {results.Average(result => result.report.RocksPerKilometer):0} "
                + $"| {results.Average(result => result.field.Metrics.DodgesPerMinute):0.0}";
        }
    }
}
