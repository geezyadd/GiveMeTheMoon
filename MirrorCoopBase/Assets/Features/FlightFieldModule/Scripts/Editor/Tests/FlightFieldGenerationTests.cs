using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Generation rules on many seeds and loops, with the game's configuration and catalog. Every check rebuilds the
    // occupied cells from the placement list, the way a participant builds the field.
    public sealed class FlightFieldGenerationTests {
        private const int SEED_COUNT = 200;
        private const int FIRST_SEED = 1;

        private static readonly int[] _loops = { 0, 3, 10 };

        private readonly Dictionary<int, List<FlightField>> _fieldsByLoop = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            foreach (int loop in _loops)
                _fieldsByLoop[loop] = Enumerable.Range(FIRST_SEED, SEED_COUNT).Select(seed => Generate(seed, loop)).ToList();
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenEveryRouteHasAPassageWithClearance(int loop) {
            int clearance = _composition.Configuration.PassageClearanceCells;
            foreach (FlightField field in _fieldsByLoop[loop]) {
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                foreach (FieldRoute route in field.Routes)
                    Assert.IsTrue(occupancy.HasPassage(route.Start, field.StationBCell, clearance), $"Seed {field.Seed}, loop {loop}: route {route.Id} has no passage to B.");
            }
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenRoutesLeaveTheTakeoffCorridorAndEndAtB(int loop) {
            foreach (FlightField field in _fieldsByLoop[loop]) {
                foreach (FieldRoute route in field.Routes) {
                    bool isAtCruiseStart = route.Start == field.CruiseStartCell || field.Cells[route.Start].Reserve == FieldReserveKind.TakeoffCorridor;
                    Assert.IsTrue(isAtCruiseStart, $"Seed {field.Seed}, loop {loop}: route {route.Id} does not start in the takeoff corridor.");
                    Assert.AreEqual(field.StationBCell, route.End, $"Seed {field.Seed}, loop {loop}: route {route.Id} does not end at B.");
                }
            }
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenStationZonesAndTakeoffCorridorAreEmpty(int loop) {
            for (int i = 0; i < SEED_COUNT; i++) {
                FlightField field = _fieldsByLoop[loop][i];
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                FieldRequest request = CreateRequest(FIRST_SEED + i, loop);
                for (int x = 0; x < occupancy.Size.x; x++) {
                    for (int y = 0; y < occupancy.Size.y; y++)
                        AssertNotInEmptyZone(field, occupancy, request, new Vector2Int(x, y));
                }
            }
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenPlacementsDoNotOverlapAndStayInside(int loop) {
            foreach (FlightField field in _fieldsByLoop[loop]) {
                Assert.IsNotEmpty(field.Placements, $"Seed {field.Seed}, loop {loop}: the field is empty.");
                CreateOccupancy(field);
            }
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenTheBorderWallHasNoHolesOutsideReservedCells(int loop) {
            foreach (FlightField field in _fieldsByLoop[loop]) {
                FieldTestOccupancy occupancy = CreateOccupancy(field);
                foreach (Vector2Int cell in GetBorderCells(occupancy.Size)) {
                    bool isClosed = occupancy.IsOccupied(cell) || field.Cells[cell].IsReserved;
                    Assert.IsTrue(isClosed, $"Seed {field.Seed}, loop {loop}: hole in the wall at {cell}.");
                }
            }
        }

        [TestCaseSource(nameof(_loops))]
        public void WhenFieldsAreGenerated_ThenNoPassageWasForcedAndRocksOfThreeSizesStand(int loop) {
            foreach (FlightField field in _fieldsByLoop[loop])
                Assert.IsFalse(field.IsPassageForced, $"Seed {field.Seed}, loop {loop}: the passage was forced.");

            int sizes = _fieldsByLoop[loop].SelectMany(field => field.Placements).Select(placement => placement.StructureId).Distinct().Count();
            Assert.GreaterOrEqual(sizes, 3, $"Loop {loop}: fewer than three rock sizes in all fields.");
        }

        [Test]
        public void WhenSameSeedIsGeneratedTwice_ThenPlacementsAreEqual() {
            foreach (int loop in _loops) {
                FlightField again = Generate(FIRST_SEED, loop);
                CollectionAssert.AreEqual(_fieldsByLoop[loop][0].Placements, again.Placements, $"Loop {loop}: seed {FIRST_SEED} gave another field.");
            }
        }

        [Test]
        public void WhenSeedsDiffer_ThenPlacementsDiffer() {
            List<FlightField> fields = _fieldsByLoop[_loops[0]];
            for (int i = 1; i < fields.Count; i++)
                CollectionAssert.AreNotEqual(fields[i - 1].Placements, fields[i].Placements, $"Seeds {fields[i - 1].Seed} and {fields[i].Seed} gave the same field.");
        }

        private FlightField Generate(int seed, int loop) =>
            _composition.Generator.Generate(CreateRequest(seed, loop));

        private FieldRequest CreateRequest(int seed, int loop) =>
            _scenario.CreateRequest(_composition.RequestFactory, seed, loop);

        private FieldTestOccupancy CreateOccupancy(FlightField field) =>
            new(field, _composition.Catalog, _composition.FootprintReader);

        private void AssertNotInEmptyZone(FlightField field, FieldTestOccupancy occupancy, FieldRequest request, Vector2Int cell) {
            if (!occupancy.IsOccupied(cell))
                return;

            Vector2 center = ToPlane(field.Frame.CellToWorld(cell));
            Vector2 stationA = ToPlane(request.StationA);
            Assert.Greater(Vector2.Distance(center, stationA), _composition.Configuration.StationAZoneRadius, $"Seed {field.Seed}: structure at {cell} in the zone of A.");
            Assert.Greater(Vector2.Distance(center, ToPlane(request.StationB)), _composition.Configuration.StationBZoneRadius, $"Seed {field.Seed}: structure at {cell} in the zone of B.");
            Assert.Greater(DistanceToSegment(center, stationA, TakeoffCorridorEnd(request)), _composition.Configuration.TakeoffCorridorHalfWidth,
                $"Seed {field.Seed}: structure at {cell} in the takeoff corridor.");
        }

        // The corridor runs on along the takeoff heading past the cruise start: the autopilot keeps that heading.
        private Vector2 TakeoffCorridorEnd(FieldRequest request) {
            Vector2 cruiseStart = ToPlane(request.CruiseStart);
            Vector2 takeoffForward = (cruiseStart - ToPlane(request.StationA)).normalized;
            return cruiseStart + takeoffForward * _composition.Configuration.TakeoffRunOutLength;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end) {
            Vector2 segment = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * along);
        }

        private static Vector2 ToPlane(Vector3 point) =>
            new(point.x, point.z);

        private static IEnumerable<Vector2Int> GetBorderCells(Vector2Int size) {
            for (int x = 0; x < size.x; x++) {
                yield return new Vector2Int(x, 0);
                yield return new Vector2Int(x, size.y - 1);
            }

            for (int y = 0; y < size.y; y++) {
                yield return new Vector2Int(0, y);
                yield return new Vector2Int(size.x - 1, y);
            }
        }
    }
}
