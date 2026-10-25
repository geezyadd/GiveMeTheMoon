using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The obstacle field guarantees on a few fields, generated once for the whole fixture: a few seeds at loops 0, 4
    // and 9 with the profile the game picks, and every obstacle profile forced at the same loops. Every rock is laid
    // out as the peers build it. The station zones and the takeoff corridor stay empty, nothing overlaps (footprints,
    // the giants' gap, the ship gap between any two plane rocks), every field has giants and debris of its profile's
    // classes, every debris rock crosses the plane, a ship finds a way from the cruise start to B through gaps it fits,
    // and the same request gives the same field and rocks.
    public sealed class FieldObstacleFieldTests {
        private const int FORCED_PROFILE_SEED = 7;
        // A step between two neighbouring grid points is not measured on its own: it may cut a rock's curve this much.
        private const float STEP_CUT_METERS = 0.25f;
        private const float GAP_TOLERANCE = 0.01f;
        private const float SIZE_TOLERANCE = 0.01f;
        private static readonly int[] _seeds = { 1, 17, 4242, 90001 };
        private static readonly int[] _loops = { 0, 4, 9 };

        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;
        private FieldObstacleSample[] _samples;

        private static IEnumerable<int> Loops => _loops;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            List<FieldRequest> requests = CreateRequests();
            _samples = new FieldObstacleSample[requests.Count];
            Parallel.For(0, requests.Count, index => _samples[index] = CreateSample(requests[index]));
        }

        [Test]
        public void WhenTheConfigurationIsRead_ThenItHasTheFourObstacleVariants() =>
            CollectionAssert.AreEquivalent(new[] { "obstacles_sparse", "obstacles_medium", "obstacles_dense", "obstacles_giants" },
                _composition.Configuration.DifficultyProfiles.Select(profile => profile.Id));

        [Test]
        public void WhenFieldsAreGenerated_ThenTheStationZonesAndTheTakeoffCorridorAreEmpty() =>
            AssertNoFailures(FindFilledEmptyAreas);

        [Test]
        public void WhenFieldsAreGenerated_ThenNoTwoGiantsOverlapOrComeCloserThanTheirGap() =>
            AssertNoFailures(FindOverlappingGiants);

        [Test]
        public void WhenFieldsAreGenerated_ThenEveryTwoPlaneRocksLeaveTheShipGap() =>
            AssertNoFailures(FindNarrowPlaneGaps);

        [Test]
        public void WhenFieldsAreGenerated_ThenEveryFieldHasGiantsAndDebris() =>
            AssertNoFailures(FindMissingObstacles);

        [Test]
        public void WhenFieldsAreGenerated_ThenGiantsAndDebrisAreOfTheProfilesClasses() =>
            AssertNoFailures(FindForeignObstacles);

        [Test]
        public void WhenFieldsAreGenerated_ThenEveryDebrisRockCrossesThePlane() =>
            AssertNoFailures(FindOffPlaneRocks);

        [Test]
        public void WhenFieldsAreGenerated_ThenAShipFindsAWayToBThroughGapsItFits() =>
            AssertNoFailures(FindBlockedWays);

        [TestCaseSource(nameof(Loops))]
        public void WhenTheSameRequestIsGeneratedTwice_ThenTheFieldAndItsRocksAreEqual(int loop) {
            FieldObstacleSample first = _samples.First(sample => sample.Field.Seed == _seeds[0] && sample.Field.LoopIndex == loop);
            FieldObstacleSample again = CreateSample(first.Request);

            Assert.AreEqual(FieldObstacleFingerprint.Create(first.Field), FieldObstacleFingerprint.Create(again.Field), $"{first.Name}: the second field differs.");
            CollectionAssert.AreEqual(first.Report.Rocks, again.Report.Rocks, $"{first.Name}: the second field lays out other rocks.");
        }

        private List<FieldRequest> CreateRequests() {
            List<FieldRequest> requests = new();
            foreach (int loop in _loops) {
                foreach (int seed in _seeds)
                    requests.Add(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

                foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles)
                    requests.Add(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, FORCED_PROFILE_SEED, loop), profile));
            }

            return requests;
        }

        // One analyzer and gap meter per call: their workspaces are not shared between threads.
        private FieldObstacleSample CreateSample(FieldRequest request) {
            FlightField field = _composition.Generator.Generate(request);
            FieldSlalomReport report = new FieldSlalomAnalyzer(_composition).Analyze(field);
            float planeGap = new FieldPlaneGapMeter().Measure(field, report.Rocks);
            return new FieldObstacleSample(request, field, _composition.ProfileLookup.GetRequired(field.ProfileId), report, planeGap);
        }

        private void AssertNoFailures(Func<FieldObstacleSample, IEnumerable<string>> findFailures) {
            List<string> failures = _samples.SelectMany(findFailures).ToList();
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // Every cell whose middle lies in a station zone or the takeoff corridor is reserved; no reserved cell holds a
        // giant or debris, and no laid-out rock reaches into one.
        private IEnumerable<string> FindFilledEmptyAreas(FieldObstacleSample sample) {
            FlightField field = sample.Field;
            foreach (Vector2Int cell in field.Cells.GetIndexes()) {
                FieldCell content = field.Cells[cell];
                if (IsInEmptyArea(sample, cell) && !content.IsReserved)
                    yield return $"{sample.Name}: cell {cell} in a station zone or the takeoff corridor is not reserved.";

                if (content.IsReserved && (content.IsOccupied || content.HasDebris))
                    yield return $"{sample.Name}: reserved cell {cell} holds a giant or debris.";
            }

            foreach (FieldSlalomRock rock in sample.Report.Rocks) {
                if (TryFindReservedCellUnder(field, rock, out Vector2Int cell))
                    yield return $"{sample.Name}: a rock of {rock.Radius * 2f:0.0} m at {rock.Position} reaches into reserved cell {cell}.";
            }
        }

        private bool IsInEmptyArea(FieldObstacleSample sample, Vector2Int cell) {
            FlightFieldConfiguration configuration = _composition.Configuration;
            FieldRequest request = sample.Request;
            Vector2 middle = FieldPlaneGeometry.ToGround(sample.Field.Frame.CellToWorld(cell));
            Vector2 stationA = FieldPlaneGeometry.ToGround(request.StationA);
            bool isInStationZone = Vector2.Distance(middle, stationA) <= configuration.StationAZoneRadius
                                   || Vector2.Distance(middle, FieldPlaneGeometry.ToGround(request.StationB)) <= configuration.StationBZoneRadius;
            float toCorridor = FieldPlaneGeometry.DistanceToSegment(middle, stationA, FieldPlaneGeometry.ToGround(CorridorEnd(request)));
            return isInStationZone || toCorridor <= configuration.TakeoffCorridorHalfWidth;
        }

        // The corridor runs from A along the takeoff heading past the cruise start (FieldTakeoffCorridorStep).
        private Vector3 CorridorEnd(FieldRequest request) {
            Vector3 heading = request.CruiseStart - request.StationA;
            heading.y = 0f;
            return request.CruiseStart + heading.normalized * _composition.Configuration.TakeoffRunOutLength;
        }

        private static bool TryFindReservedCellUnder(FlightField field, FieldSlalomRock rock, out Vector2Int reservedCell) {
            reservedCell = default;
            float cellSize = field.Frame.CellSize;
            int reach = Mathf.CeilToInt(rock.Radius / cellSize) + 1;
            foreach (Vector2Int cell in CellsAround(FieldPlaneCells.ToCell(rock.Position, cellSize), reach)) {
                if (!IsReservedUnderRock(field, rock, cell))
                    continue;

                reservedCell = cell;
                return true;
            }

            return false;
        }

        private static bool IsReservedUnderRock(FlightField field, FieldSlalomRock rock, Vector2Int cell) =>
            field.Cells.ValidateIndex(cell) && field.Cells[cell].IsReserved
                                            && FieldPlaneCells.DistanceToCell(rock.Position, cell, field.Frame.CellSize) < rock.Radius - GAP_TOLERANCE;

        // Every placement stands on exactly its footprint's cells, and no cell of another giant lies within the
        // profile's gap cells around them.
        private IEnumerable<string> FindOverlappingGiants(FieldObstacleSample sample) {
            FlightField field = sample.Field;
            int footprintCells = 0;
            foreach (FieldPlacement placement in field.Placements) {
                FieldFootprint footprint = _composition.FootprintReader.Read(_composition.Catalog.GetRequired(placement.StructureId), placement.Rotation);
                footprintCells += footprint.Cells.Count;
                foreach (Vector2Int position in footprint.Cells.Select(cell => placement.Anchor + cell).Where(position => !IsOwnedBy(field, placement, position)))
                    yield return $"{sample.Name}: placement {placement.Id} ({placement.StructureId}) does not own its footprint cell {position}.";
            }

            int occupiedCells = field.Cells.GetIndexes(IsOccupied).Count();
            if (occupiedCells != footprintCells)
                yield return $"{sample.Name}: {occupiedCells} occupied cells for {footprintCells} footprint cells: giants overlap or a cell has no giant.";

            foreach (Vector2Int cell in field.Cells.GetIndexes(IsOccupied)) {
                if (IsOtherGiantAround(field, cell, sample.Profile.GiantGapCells))
                    yield return $"{sample.Name}: another giant lies within {sample.Profile.GiantGapCells} cells of cell {cell}.";
            }
        }

        private static bool IsOwnedBy(FlightField field, FieldPlacement placement, Vector2Int cell) =>
            field.Cells.ValidateIndex(cell) && field.Cells[cell].PlacementId == placement.Id;

        private static bool IsOtherGiantAround(FlightField field, Vector2Int center, int gapCells) {
            int placement = field.Cells[center].PlacementId;
            return CellsAround(center, gapCells).Any(cell => IsOtherGiantCell(field, cell, placement));
        }

        private static bool IsOtherGiantCell(FlightField field, Vector2Int cell, int placement) =>
            field.Cells.ValidateIndex(cell) && field.Cells[cell].IsOccupied && field.Cells[cell].PlacementId != placement;

        // The square of cells within the reach around the center, in or out of the field.
        private static IEnumerable<Vector2Int> CellsAround(Vector2Int center, int reach) {
            for (int x = center.x - reach; x <= center.x + reach; x++) {
                for (int y = center.y - reach; y <= center.y + reach; y++)
                    yield return new Vector2Int(x, y);
            }
        }

        private IEnumerable<string> FindNarrowPlaneGaps(FieldObstacleSample sample) {
            float minGap = _composition.Configuration.DebrisMinGapMeters - GAP_TOLERANCE;
            if (sample.PlaneGapMeters < minGap)
                yield return $"{sample.Name}: two obstacles in the flight plane are only {sample.PlaneGapMeters:0.0} m apart.";
        }

        private static IEnumerable<string> FindMissingObstacles(FieldObstacleSample sample) {
            if (sample.Field.Placements.Count == 0)
                yield return $"{sample.Name}: the field has no giant.";

            if (!sample.Field.Cells.GetIndexes(HasDebris).Any())
                yield return $"{sample.Name}: the field has no debris cell.";

            if (sample.Report.Rocks.Count == 0)
                yield return $"{sample.Name}: the field lays out no debris rock.";
        }

        private static IEnumerable<string> FindForeignObstacles(FieldObstacleSample sample) {
            FieldDifficultyProfile profile = sample.Profile;
            HashSet<string> giantIds = new(profile.GiantClasses.SelectMany(giantClass => giantClass.Obstacles).Select(obstacle => obstacle.Structure.Id));
            foreach (FieldPlacement placement in sample.Field.Placements) {
                if (!giantIds.Contains(placement.StructureId))
                    yield return $"{sample.Name}: giant {placement.StructureId} is not one of the profile's giant obstacles.";
            }

            HashSet<string> debrisIds = new(profile.DebrisTypes.Select(option => option.Debris.Id));
            foreach (Vector2Int cell in sample.Field.Cells.GetIndexes(HasDebris)) {
                if (!debrisIds.Contains(sample.Field.Cells[cell].DebrisId))
                    yield return $"{sample.Name}: debris {sample.Field.Cells[cell].DebrisId} in cell {cell} is not one of the profile's debris types.";
            }

            foreach (FieldSlalomRock rock in sample.Report.Rocks) {
                if (!IsOfSizeClass(profile, rock.Radius * 2f))
                    yield return $"{sample.Name}: a debris rock of {rock.Radius * 2f:0.00} m is in none of the profile's size classes.";
            }
        }

        private static bool IsOfSizeClass(FieldDifficultyProfile profile, float diameter) =>
            profile.DebrisSizeClasses.Any(sizeClass => diameter >= sizeClass.DiameterMeters.x - SIZE_TOLERANCE && diameter <= sizeClass.DiameterMeters.y + SIZE_TOLERANCE);

        private static IEnumerable<string> FindOffPlaneRocks(FieldObstacleSample sample) {
            if (!sample.Report.Rocks.All(rock => rock.IsInPlane))
                yield return $"{sample.Name}: a debris rock stands off the plane, inside the volume's clearance band.";
        }

        private IEnumerable<string> FindBlockedWays(FieldObstacleSample sample) {
            float minGap = _composition.Configuration.DebrisMinGapMeters - 2f * (FieldSlalomPathFinder.ToleranceMeters + STEP_CUT_METERS);
            if (!sample.Report.IsPathFound)
                yield return $"{sample.Name}: no way for the ship from the cruise start to B.";
            else if (sample.Report.MinGapMeters < minGap)
                yield return $"{sample.Name}: the way passes a gap of {sample.Report.MinGapMeters:0.0} m.";
        }

        private static bool IsOccupied(FieldCell cell) =>
            cell.IsOccupied;

        private static bool HasDebris(FieldCell cell) =>
            cell.HasDebris;
    }
}
