using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Routes as entities: difficulty reaches the corridor through IFieldRouteShaper, several routes per field, the
    // forced passage, and the generation time of a 40 x 40 field.
    public sealed class FlightFieldRouteTests {
        private const float EASY = 0f;
        private const float HARD = 1f;
        private const int SEED = 17;
        private const int TIMING_SEEDS = 20;
        private const int TIMING_LENGTH_CELLS = 40;
        private const double MAX_AVERAGE_MILLISECONDS = 50.0;
        private const float TAKEOFF_FORWARD = 350f;
        private const float TAKEOFF_HEIGHT = 100f;
        private const string FORCED_PASSAGE_LOG = "no passage after";
        private const string OBSTACLE_DENSITY_FIELD = "_obstacleDensity";
        private const string MAX_REPAIR_ROUNDS_FIELD = "_maxRepairRounds";
        private const string MAX_SEED_ATTEMPTS_FIELD = "_maxSeedAttempts";
        private const string CORRIDOR_OBSTACLE_SHARE_FIELD = "_routeShape._corridorObstacleShare";
        private const string TAKEOFF_RUN_OUT_LENGTH_FIELD = "_takeoffRunOutLength";

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenTwoDifficultiesAreShaped_ThenCorridorsHaveDifferentWidths() {
            CurveFieldRouteShaper shaper = new(_composition.Configuration);

            Assert.AreNotEqual(shaper.Shape(EASY).WidthCells, shaper.Shape(HARD).WidthCells);
        }

        [Test]
        public void WhenRoutesOfTwoDifficultiesAreGenerated_ThenTheirCorridorWidthsComeFromTheShaper() {
            CurveFieldRouteShaper shaper = new(_composition.Configuration);

            FlightField easyField = _composition.Generator.Generate(CreateStraightRequest(SEED, EASY));
            FlightField hardField = _composition.Generator.Generate(CreateStraightRequest(SEED, HARD));

            Assert.AreEqual(shaper.Shape(EASY).WidthCells, easyField.Routes[0].CorridorWidthCells);
            Assert.AreEqual(shaper.Shape(HARD).WidthCells, hardField.Routes[0].CorridorWidthCells);
            Assert.Greater(easyField.Routes[0].CorridorWidthCells, hardField.Routes[0].CorridorWidthCells);
        }

        [Test]
        public void WhenRequestHasTwoRoutes_ThenFieldKeepsBothAndEachHasAPassage() {
            FlightField field = _composition.Generator.Generate(CreateStraightRequest(SEED, EASY, HARD));

            Assert.AreEqual(2, field.Routes.Count);
            FieldTestOccupancy occupancy = new(field, _composition.Catalog, _composition.FootprintReader);
            foreach (FieldRoute route in field.Routes)
                Assert.IsTrue(occupancy.HasPassage(route.Start, field.StationBCell, _composition.Configuration.PassageClearanceCells), $"Route {route.Id} has no passage.");
        }

        [Test]
        public void WhenRequestIsCreatedForALoop_ThenRouteDifficultiesComeFromTheLatestLoopEntry() {
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _composition.Configuration.RoutesByLoop) {
                FieldRequest request = _composition.RequestFactory.Create(Vector3.zero, Vector3.forward, Vector3.zero, loopRoutes.FromLoop, SEED);

                CollectionAssert.AreEqual(loopRoutes.RouteDifficulties, request.RouteRequests.Select(route => route.Difficulty).ToArray(), $"Loop {loopRoutes.FromLoop}.");
            }
        }

        [Test]
        public void WhenNoSeedGivesAPassage_ThenCorridorIsClearedAndAnErrorIsLogged() {
            FlightFieldConfiguration blockingConfiguration = CreateBlockingConfiguration();
            try {
                FlightFieldEditorComposition composition = new FlightFieldEditorCompositionFactory().Create(blockingConfiguration);
                LogAssert.Expect(LogType.Error, new Regex(FORCED_PASSAGE_LOG));

                FlightField field = composition.Generator.Generate(CreateStraightRequest(SEED, HARD));

                Assert.IsTrue(field.IsPassageForced);
                FieldTestOccupancy occupancy = new(field, composition.Catalog, composition.FootprintReader);
                Assert.IsTrue(occupancy.HasPassage(field.Routes[0].Start, field.StationBCell, blockingConfiguration.PassageClearanceCells));
            } finally {
                Object.DestroyImmediate(blockingConfiguration);
            }
        }

        [Test]
        public void WhenFieldIs40CellsLong_ThenGenerationIsFasterThanTheThreshold() {
            float distance = (TIMING_LENGTH_CELLS - _composition.Configuration.MarginBehindACells - _composition.Configuration.MarginBehindBCells - 1) * _composition.Configuration.CellSize
                + _composition.Configuration.CellSize / 2f;
            Assert.AreEqual(TIMING_LENGTH_CELLS, _composition.Generator.Generate(CreateStraightRequestAt(SEED, distance, EASY)).Frame.LengthCells);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int seed = 0; seed < TIMING_SEEDS; seed++)
                _composition.Generator.Generate(CreateStraightRequestAt(seed, distance, EASY));

            double averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / TIMING_SEEDS;
            Assert.Less(averageMilliseconds, MAX_AVERAGE_MILLISECONDS, $"A 40-cell long field takes {averageMilliseconds:0.0} ms on average.");
        }

        private static FieldRequest CreateStraightRequest(int seed, params float[] difficulties) =>
            CreateStraightRequestAt(seed, TAKEOFF_FORWARD * 2f, difficulties);

        private static FieldRequest CreateStraightRequestAt(int seed, float distance, params float[] difficulties) {
            List<FieldRouteRequest> routeRequests = difficulties.Select(difficulty => new FieldRouteRequest(difficulty)).ToList();
            Vector3 cruiseStart = new(0f, TAKEOFF_HEIGHT, TAKEOFF_FORWARD);
            return new FieldRequest(Vector3.zero, Vector3.forward * distance, cruiseStart, 0, seed, routeRequests);
        }

        // Every free cell filled, obstacles filling whole corridors, no takeoff run-out, no repair and one seed: only the
        // forced clearing can give a passage.
        private FlightFieldConfiguration CreateBlockingConfiguration() {
            FlightFieldConfiguration configuration = Object.Instantiate(_composition.Configuration);
            SerializedObject serializedConfiguration = new(configuration);
            serializedConfiguration.FindProperty(OBSTACLE_DENSITY_FIELD).floatValue = 1f;
            serializedConfiguration.FindProperty(MAX_REPAIR_ROUNDS_FIELD).intValue = 0;
            serializedConfiguration.FindProperty(MAX_SEED_ATTEMPTS_FIELD).intValue = 1;
            serializedConfiguration.FindProperty(CORRIDOR_OBSTACLE_SHARE_FIELD).animationCurveValue = AnimationCurve.Constant(0f, 1f, 1f);
            // On this short straight route the run-out past the cruise start would reach B and give a passage by itself.
            serializedConfiguration.FindProperty(TAKEOFF_RUN_OUT_LENGTH_FIELD).floatValue = 0f;
            serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();
            return configuration;
        }
    }
}
