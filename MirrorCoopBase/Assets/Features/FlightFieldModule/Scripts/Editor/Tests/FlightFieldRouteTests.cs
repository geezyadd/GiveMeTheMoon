using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Routes as entities: the profile and the difficulty reach the corridor through IFieldRouteShaper, several main
    // routes per field, alternatives and dead ends branch off the main route, the profile is picked from the loop
    // entry, and the generation time of a 40 x 40 field.
    public sealed class FlightFieldRouteTests {
        private const float EASY = 0f;
        private const float HARD = 1f;
        private const int SEED = 17;
        private const int SEEDS = 50;
        private const int TIMING_SEEDS = 20;
        private const int TIMING_LENGTH_CELLS = 40;
        private const double MAX_AVERAGE_MILLISECONDS = 50.0;
        private const float LONG_DISTANCE = 4000f;
        private const float SHORT_DISTANCE = 700f;

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenTheEasyAndHardEndsOfAProfileAreShaped_ThenTheShapeFollowsTheProfile() {
            ProfileFieldRouteShaper shaper = new(_composition.Configuration);
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                FieldCorridorShape easy = shaper.Shape(profile, EASY);
                FieldCorridorShape hard = shaper.Shape(profile, HARD);

                Assert.AreEqual(Mathf.Max(_composition.Configuration.PassageClearanceCells, Mathf.RoundToInt(profile.CorridorHalfWidthCells.Easy)), easy.HalfWidthCells, profile.Id);
                Assert.AreEqual(Mathf.Max(_composition.Configuration.PassageClearanceCells, Mathf.RoundToInt(profile.CorridorHalfWidthCells.Hard)), hard.HalfWidthCells, profile.Id);
                Assert.AreEqual(profile.MeanderAmplitudeCells.Hard, hard.MeanderAmplitudeCells, profile.Id);
            }
        }

        [Test]
        public void WhenMainRoutesAreGenerated_ThenTheirWidthStaysAroundTheShapeAndNeverBelowTheClearance() {
            ProfileFieldRouteShaper shaper = new(_composition.Configuration);
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                FieldRoute main = _composition.Generator.Generate(FieldTestRequests.Straight(SEED, LONG_DISTANCE, profile, HARD)).Routes[0];
                FieldCorridorShape shape = shaper.Shape(profile, HARD);

                Assert.GreaterOrEqual(main.HalfWidths.Min(), _composition.Configuration.PassageClearanceCells, profile.Id);
                Assert.LessOrEqual(main.HalfWidths.Max(), shape.HalfWidthCells + Mathf.CeilToInt(shape.WidthVariationCells), profile.Id);
            }
        }

        [Test]
        public void WhenTheMainRouteIsGenerated_ThenItWindsAwayFromTheStraightLine() {
            FieldDifficultyProfile profile = _composition.Configuration.DifficultyProfiles[0];
            FlightField field = _composition.Generator.Generate(FieldTestRequests.Straight(SEED, LONG_DISTANCE, profile, EASY));
            int straightRow = field.Routes[0].Start.y;

            int widestSwing = field.Routes[0].Centerline.Max(cell => Mathf.Abs(cell.y - straightRow));

            Assert.GreaterOrEqual(widestSwing, Mathf.FloorToInt(profile.MeanderAmplitudeCells.Easy / 2f), "The main corridor does not wind.");
        }

        [Test]
        public void WhenRequestHasTwoRoutes_ThenFieldKeepsBothMainRoutesAndEachHasAPassage() {
            FieldDifficultyProfile profile = _composition.Configuration.DifficultyProfiles[0];
            FlightField field = _composition.Generator.Generate(FieldTestRequests.Straight(SEED, LONG_DISTANCE, profile, EASY, HARD));

            FieldRoute[] mains = field.Routes.Where(route => route.Kind == FieldRouteKind.Main).ToArray();
            Assert.AreEqual(2, mains.Length);
            FieldTestOccupancy occupancy = new(field, _composition.Catalog, _composition.FootprintReader);
            foreach (FieldRoute route in mains)
                Assert.IsTrue(occupancy.HasPassage(route.Start, field.StationBCell, _composition.Configuration.PassageClearanceCells), $"Route {route.Id} has no passage.");
        }

        [Test]
        public void WhenLongFieldsAreGenerated_ThenBranchesFollowTheProfileAndLeaveTheMainRoute() {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                FlightField field = _composition.Generator.Generate(FieldTestRequests.Straight(SEED, LONG_DISTANCE, profile, HARD));
                FieldRoute main = field.Routes[0];
                List<FieldRoute> alternatives = field.Routes.Where(route => route.Kind == FieldRouteKind.Alternative).ToList();
                List<FieldRoute> deadEnds = field.Routes.Where(route => route.Kind == FieldRouteKind.DeadEnd).ToList();

                Assert.AreEqual(Mathf.RoundToInt(profile.AlternativeRoutes.Hard), alternatives.Count, $"{profile.Id}: alternatives.");
                Assert.AreEqual(Mathf.RoundToInt(profile.DeadEnds.Hard), deadEnds.Count, $"{profile.Id}: dead ends.");
                foreach (FieldRoute alternative in alternatives) {
                    Assert.Contains(alternative.Start, main.Centerline.ToList(), $"{profile.Id}: alternative {alternative.Id} does not leave the main route.");
                    Assert.Contains(alternative.End, main.Centerline.ToList(), $"{profile.Id}: alternative {alternative.Id} does not rejoin the main route.");
                }

                foreach (FieldRoute deadEnd in deadEnds)
                    Assert.Contains(deadEnd.Start, main.Centerline.ToList(), $"{profile.Id}: dead end {deadEnd.Id} does not leave the main route.");
            }
        }

        [Test]
        public void WhenRequestIsCreatedForALoop_ThenDifficultiesAndProfileComeFromTheLatestLoopEntry() {
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _composition.Configuration.RoutesByLoop) {
                for (int seed = 0; seed < SEEDS; seed++) {
                    FieldRequest request = _composition.RequestFactory.Create(Vector3.zero, Vector3.forward, Vector3.zero, loopRoutes.FromLoop, seed);

                    CollectionAssert.AreEqual(loopRoutes.RouteDifficulties, request.RouteRequests.Select(route => route.Difficulty).ToArray(), $"Loop {loopRoutes.FromLoop}.");
                    Assert.IsTrue(loopRoutes.Profiles.Any(option => option.Profile == request.Profile), $"Loop {loopRoutes.FromLoop}: profile {request.Profile.Id} is not one of the entry's.");
                }
            }
        }

        [Test]
        public void WhenTheSameSeedIsRequestedTwice_ThenTheSameProfileIsPicked() {
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _composition.Configuration.RoutesByLoop) {
                for (int seed = 0; seed < SEEDS; seed++) {
                    FieldRequest first = _composition.RequestFactory.Create(Vector3.zero, Vector3.forward, Vector3.zero, loopRoutes.FromLoop, seed);
                    FieldRequest second = _composition.RequestFactory.Create(Vector3.zero, Vector3.forward, Vector3.zero, loopRoutes.FromLoop, seed);

                    Assert.AreSame(first.Profile, second.Profile, $"Loop {loopRoutes.FromLoop} seed {seed}.");
                }
            }
        }

        [Test]
        public void WhenFieldIs40CellsLong_ThenGenerationIsFasterThanTheThreshold() {
            FieldDifficultyProfile profile = _composition.Configuration.DifficultyProfiles[0];
            float distance = (TIMING_LENGTH_CELLS - _composition.Configuration.MarginBehindACells - _composition.Configuration.MarginBehindBCells - 1) * _composition.Configuration.CellSize
                + _composition.Configuration.CellSize / 2f;
            Assert.AreEqual(TIMING_LENGTH_CELLS, _composition.Generator.Generate(FieldTestRequests.Straight(SEED, distance, profile, EASY)).Frame.LengthCells);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int seed = 0; seed < TIMING_SEEDS; seed++)
                _composition.Generator.Generate(FieldTestRequests.Straight(seed, distance, profile, EASY));

            double averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / TIMING_SEEDS;
            Assert.Less(averageMilliseconds, MAX_AVERAGE_MILLISECONDS, $"A 40-cell long field takes {averageMilliseconds:0.0} ms on average.");
        }

        [Test]
        public void WhenAShortFieldIsGenerated_ThenItStillHasItsMainRoute() {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                FlightField field = _composition.Generator.Generate(FieldTestRequests.Straight(SEED, SHORT_DISTANCE, profile, HARD));

                Assert.AreEqual(FieldRouteKind.Main, field.Routes[0].Kind, profile.Id);
                Assert.IsFalse(field.IsPassageForced, profile.Id);
            }
        }
    }
}
