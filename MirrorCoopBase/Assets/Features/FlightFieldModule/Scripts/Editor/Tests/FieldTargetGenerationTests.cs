using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The target balloon step on the game's flights: every balloon stands in a passable cell, clear of every rock, of
    // the other balloons and within its height band; loop 0 gets the configuration's count; the same request gives the
    // same balloons; the stage preset's scale gives fewer or more; and the step changes no cell or placement.
    public sealed class FieldTargetGenerationTests {
        private const int SEEDS = 24;
        private const int LATE_LOOP = 5;
        private const float FEWER_SCALE = 0.5f;
        private const float MORE_SCALE = 2f;
        private const float NO_TARGETS_SCALE = 0f;
        private const float HEIGHT_TOLERANCE = 0.001f;

        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;
        private FieldRockDurabilityConfiguration _durability;
        private FieldTargetConfiguration _configuration;
        private FieldTargetCatalog _catalog;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _durability = AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default);
            _configuration = AddressableConfigurationLoader.Load<FieldTargetConfiguration>(Address.Configurations.FieldTargetConfiguration_Default);
            _catalog = AddressableConfigurationLoader.Load<FieldTargetCatalog>(Address.Configurations.FieldTargetCatalog_Default);
        }

        [TestCase(0)]
        [TestCase(LATE_LOOP)]
        public void WhenFieldsAreGenerated_ThenEveryBalloonStandsInAPassableCellClearOfRocksAndOtherBalloons(int loopIndex) {
            int balloons = 0;
            for (int seed = 0; seed < SEEDS; seed++) {
                FlightField field = Generate(seed, loopIndex, FieldRequest.NO_TARGET_COUNT_SCALE);
                FieldRockTestRig rocks = new(_composition, _durability, field);
                foreach (FieldTargetPlacement target in field.Targets)
                    AssertPlaced(field, rocks, target, $"loop {loopIndex} seed {seed}");

                AssertSpaced(field, $"loop {loopIndex} seed {seed}");
                balloons += field.Targets.Count;
            }

            Assert.Greater(balloons, 0, $"Loop {loopIndex}: no field got a balloon.");
            Debug.Log($"[FieldTargets] loop {loopIndex}: {balloons} balloons on {SEEDS} fields");
        }

        [Test]
        public void WhenLoopZeroFieldsAreGenerated_ThenEachGetsTheConfiguredCount() {
            Vector2Int range = _configuration.FirstLoopCount;
            List<int> counts = new();
            for (int seed = 0; seed < SEEDS; seed++)
                counts.Add(Generate(seed, 0, FieldRequest.NO_TARGET_COUNT_SCALE).Targets.Count);

            Debug.Log($"[FieldTargets] loop 0 counts: {string.Join(", ", counts)}");
            Assert.IsTrue(counts.All(count => count >= range.x && count <= range.y), $"A loop 0 field left the range {range}: {string.Join(", ", counts)}.");
        }

        [Test]
        public void WhenTheSameRequestIsGeneratedTwice_ThenTheBalloonsAreTheSame() {
            FlightField first = Generate(3, 1, FieldRequest.NO_TARGET_COUNT_SCALE);
            FlightField second = Generate(3, 1, FieldRequest.NO_TARGET_COUNT_SCALE);

            Assert.Greater(first.Targets.Count, 0, "The field got no balloon.");
            Assert.AreEqual(first.Targets.Count, second.Targets.Count, "The same request gave another balloon count.");
            for (int i = 0; i < first.Targets.Count; i++) {
                Assert.AreEqual(first.Targets[i].KindId, second.Targets[i].KindId, $"Balloon {i} is another kind.");
                Assert.AreEqual(first.Targets[i].FieldPosition, second.Targets[i].FieldPosition, $"Balloon {i} stands elsewhere.");
            }
        }

        [Test]
        public void WhenTheStagePresetScalesTheCount_ThenAFieldGetsFewerOrMoreBalloons() {
            int fewer = 0;
            int normal = 0;
            int more = 0;
            for (int seed = 0; seed < SEEDS; seed++) {
                fewer += Generate(seed, 0, FEWER_SCALE).Targets.Count;
                normal += Generate(seed, 0, FieldRequest.NO_TARGET_COUNT_SCALE).Targets.Count;
                more += Generate(seed, 0, MORE_SCALE).Targets.Count;
            }

            Debug.Log($"[FieldTargets] loop 0 balloons on {SEEDS} fields: scale {FEWER_SCALE} {fewer}, 1 {normal}, {MORE_SCALE} {more}");
            Assert.Less(fewer, normal, "A smaller count scale did not give fewer balloons.");
            Assert.Greater(more, normal, "A larger count scale did not give more balloons.");
        }

        [Test]
        public void WhenBalloonsArePlaced_ThenNoCellOrPlacementOfTheFieldChanges() {
            for (int seed = 0; seed < SEEDS; seed += 4) {
                FlightField without = Generate(seed, 2, NO_TARGETS_SCALE);
                FlightField with = Generate(seed, 2, MORE_SCALE);

                Assert.AreEqual(0, without.Targets.Count, $"Seed {seed}: a count scale of 0 still placed balloons.");
                Assert.AreEqual(without.Placements.Count, with.Placements.Count, $"Seed {seed}: the balloons changed the placements.");
                AssertSameCells(without, with, seed);
            }
        }

        private static void AssertSameCells(FlightField without, FlightField with, int seed) {
            Vector2Int size = with.Frame.Size;
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++)
                    Assert.IsTrue(IsSameCell(without.Cells[x, y], with.Cells[x, y]), $"Seed {seed}: the balloons changed cell ({x}, {y}).");
            }
        }

        private static bool IsSameCell(FieldCell before, FieldCell after) =>
            before.Class == after.Class && before.Reserve == after.Reserve && before.PlacementId == after.PlacementId && before.DebrisId == after.DebrisId
            && Mathf.Approximately(before.DebrisDensity, after.DebrisDensity);

        private void AssertPlaced(FlightField field, FieldRockTestRig rocks, FieldTargetPlacement target, string where) {
            Assert.IsTrue(_catalog.TryGet(target.KindId, out FieldTargetDefinition kind), $"{where}: balloon kind '{target.KindId}' is not in the catalog.");
            Vector2Int cell = new(Mathf.FloorToInt(target.FieldPosition.z / field.Frame.CellSize), Mathf.FloorToInt(target.FieldPosition.x / field.Frame.CellSize));
            Assert.IsTrue(field.Cells.ValidateIndex(cell), $"{where}: a balloon stands outside the field ({cell}).");
            FieldCell fieldCell = field.Cells[cell];
            Assert.IsTrue(fieldCell.IsFree && !fieldCell.IsBlockedClass, $"{where}: a balloon stands in cell {cell} that is not passable ({fieldCell.Class}, {fieldCell.Reserve}).");
            Assert.That(target.FieldPosition.y, Is.InRange(_configuration.HeightMeters.x - HEIGHT_TOLERANCE, _configuration.HeightMeters.y + HEIGHT_TOLERANCE), $"{where}: a balloon left its height band.");

            List<FieldRockDescription> touching = new();
            Vector3 world = field.Frame.Origin + Quaternion.LookRotation(field.Frame.Forward, Vector3.up) * target.FieldPosition;
            rocks.Query.FindInSphere(world, kind.RadiusMeters + _configuration.RockClearanceMeters, touching);
            Assert.IsEmpty(touching, $"{where}: balloon {target.KindId} at {world} is within {_configuration.RockClearanceMeters} m of rock {touching.FirstOrDefault().Id}.");
        }

        private void AssertSpaced(FlightField field, string where) {
            for (int i = 0; i < field.Targets.Count; i++) {
                for (int j = i + 1; j < field.Targets.Count; j++) {
                    float distance = (field.Targets[i].FieldPosition - field.Targets[j].FieldPosition).magnitude;
                    Assert.GreaterOrEqual(distance, _configuration.MinSpacingMeters, $"{where}: balloons {i} and {j} stand {distance:0.0} m apart.");
                }
            }
        }

        private FlightField Generate(int seed, int loopIndex, float countScale) {
            FieldRequest request = _scenario.CreateRequest(_composition.RequestFactory, seed, loopIndex);
            return _composition.Generator.Generate(new FieldRequest(request.StationA, request.StationB, request.CruiseStart, request.LoopIndex, request.Seed,
                request.RouteRequests, request.Profile, countScale));
        }
    }
}
