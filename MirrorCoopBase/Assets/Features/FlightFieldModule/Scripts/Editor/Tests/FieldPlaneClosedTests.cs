using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The flight plane is closed: every field of every profile is laid out as a peer builds it (the cluster rocks of
    // every placement), and points of the flight plane slab (half the ship's height and a margin up and down) are
    // sampled inside every occupied cell and on the edges between occupied cells, at most the allowed gap apart; each
    // must lie inside the mesh bounds of a rock. The main centerline stays clear of rocks.
    public sealed class FieldPlaneClosedTests {
        private const int FIRST_SEED = 1;
        private const int SEEDS = 2;
        private const int LOOP = 0;
        private const float TOLERANCE = 0.001f;
        private const int TURNS_SAMPLED = 500;
        private const float SQUARE_ROOT_OF_THREE = 1.7320508f;
        private const float CELL_CENTER = 0.5f;
        // A large prime keeps the visual sequences of neighbouring placements apart (like the scene builder).
        private const int VISUAL_SEED_STEP = 486187739;

        private readonly FieldRockMeshBounds _meshBounds = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;
        private FieldClusterLayout _clusterLayout;
        private FieldStructureLayout _structureLayout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _clusterLayout = new FieldClusterLayout(new FieldPlaneCoverLayout());
            _structureLayout = new FieldStructureLayout(_composition.FootprintReader);
        }

        [Test]
        public void WhenRecipesAreRead_ThenPlaneRocksAreAsWideAndThickAsTheRecipesSay() {
            foreach (FieldStructureDefinition structure in _composition.Catalog.Structures) {
                FieldClusterRecipe recipe = structure.ClusterRecipe;
                Assert.IsTrue(recipe.HasPlaneCover, $"{recipe.name} does not close the flight plane.");
                foreach (FieldClusterRockOption option in recipe.PlaneRocks)
                    AssertPlaneRockFits(recipe, option.Prefab);
            }
        }

        [Test]
        public void WhenFieldsAreLaidOut_ThenTheFlightPlaneIsClosedInAndBetweenOccupiedCells() {
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                for (int seed = FIRST_SEED; seed < FIRST_SEED + SEEDS; seed++) {
                    FlightField field = Generate(profile, seed);
                    FieldRockBoundsIndex rocks = LayOutRocks(field);
                    AssertOccupiedCellsClosed(field, rocks);
                }
            }
        }

        [Test]
        public void WhenFieldsAreLaidOut_ThenTheMainCenterlineIsClearOfRocks() {
            float halfShip = _composition.Configuration.ShipWidthMeters / 2f;
            foreach (FieldDifficultyProfile profile in _composition.Configuration.DifficultyProfiles) {
                FlightField field = Generate(profile, FIRST_SEED);
                FieldRockBoundsIndex rocks = LayOutRocks(field);
                foreach (Vector2Int cell in field.Routes[0].Centerline) {
                    Vector3 center = CellPoint(cell, CELL_CENTER, CELL_CENTER, 0f, field.Frame.CellSize);
                    Assert.IsFalse(rocks.IsWithin(center, 0f) || rocks.IsWithin(center + Vector3.right * halfShip, 0f) || rocks.IsWithin(center + Vector3.left * halfShip, 0f),
                        $"{profile.Id} seed {field.Seed}: a rock stands on the main centerline at {cell}.");
                }
            }
        }

        private FlightField Generate(FieldDifficultyProfile profile, int seed) =>
            _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, LOOP), profile));

        // A dense-field rock turned any way covers its part by its smallest one-sided extent, reaches at most its reach,
        // and closes the plane slab whatever its height jitter.
        private void AssertPlaneRockFits(FieldClusterRecipe recipe, FieldRockVisual prefab) {
            Random.InitState(prefab.name.Length);
            for (int turn = 0; turn < TURNS_SAMPLED; turn++) {
                Bounds bounds = _meshBounds.Get(prefab, Random.rotationUniform, Vector3.one);
                float extent = Mathf.Min(Mathf.Min(Mathf.Min(-bounds.min.x, bounds.max.x), Mathf.Min(-bounds.min.z, bounds.max.z)), Mathf.Min(-bounds.min.y, bounds.max.y));
                Assert.GreaterOrEqual(extent + TOLERANCE, recipe.PlaneHalfExtentShare, $"{recipe.name}: {prefab.name} is narrower than the plane half-extent share in some turn.");
                Assert.LessOrEqual(bounds.extents.magnitude, recipe.PlaneReachShare * SQUARE_ROOT_OF_THREE + TOLERANCE, $"{recipe.name}: {prefab.name} bounds are too large.");
            }

            Assert.GreaterOrEqual(recipe.PlaneMinHalfHeightMeters - recipe.PlaneHeightJitterMeters, _composition.Configuration.PlaneHalfThicknessMeters,
                $"{recipe.name}: the smallest rock does not close the plane slab.");
            Assert.LessOrEqual(recipe.PlaneGapMeters.y, _composition.Configuration.MaxPlaneGapMeters, $"{recipe.name}: the plane gap is wider than the configured one.");
        }

        private FieldRockBoundsIndex LayOutRocks(FlightField field) {
            FieldRockBoundsIndex index = new(field.Frame.CellSize, _composition.Configuration.MaxPlaneGapMeters);
            List<FieldClusterRockPose> rocks = new();
            for (int i = 0; i < field.Placements.Count; i++) {
                FieldPlacement placement = field.Placements[i];
                FieldStructureDefinition structure = _composition.Catalog.GetRequired(placement.StructureId);
                FieldFootprint footprint = _composition.FootprintReader.Read(structure, placement.Rotation);
                Pose pose = _structureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, field.Frame.CellSize);
                _clusterLayout.Fill(structure.ClusterRecipe, footprint, field.Frame.CellSize, new FieldRandom(unchecked(field.Seed + i * VISUAL_SEED_STEP)), rocks);
                foreach (FieldClusterRockPose rock in rocks) {
                    Bounds bounds = _meshBounds.Get(rock.Prefab, rock.Rotation, rock.Scale);
                    bounds.center += pose.position + rock.Offset;
                    index.Add(bounds);
                }
            }

            return index;
        }

        private void AssertOccupiedCellsClosed(FlightField field, FieldRockBoundsIndex rocks) {
            float cellSize = field.Frame.CellSize;
            int steps = Mathf.CeilToInt(cellSize / _composition.Configuration.MaxPlaneGapMeters);
            float halfThickness = _composition.Configuration.PlaneHalfThicknessMeters;
            // A point within half the allowed gap of a rock's bounds is closed: any open gap is narrower than the ship.
            float gapReach = _composition.Configuration.MaxPlaneGapMeters / 2f;
            foreach (Vector2Int cell in field.Cells.GetIndexes(IsOccupied)) {
                for (int u = 0; u <= steps; u++) {
                    for (int v = 0; v <= steps; v++)
                        AssertColumnClosed(field, rocks, cell, new Vector2(u / (float)steps, v / (float)steps), new Vector2(halfThickness, gapReach));
                }
            }
        }

        // An edge point is checked only when the cell beyond the edge is occupied too: a free neighbour may stay open.
        // Slab: x = half thickness of the plane slab, y = how far from a rock's bounds a point still counts as closed.
        private static void AssertColumnClosed(FlightField field, FieldRockBoundsIndex rocks, Vector2Int cell, Vector2 share, Vector2 slab) {
            if (!IsBetweenOccupiedCells(field, cell, share))
                return;

            foreach (float height in new[] { -slab.x, 0f, slab.x }) {
                Vector3 point = CellPoint(cell, share.x, share.y, height, field.Frame.CellSize);
                if (!rocks.IsWithin(point, slab.y))
                    Assert.Fail($"{field.ProfileId} seed {field.Seed}: the flight plane is open in cell {cell} at {share} height {height}.");
            }
        }

        private static bool IsBetweenOccupiedCells(FlightField field, Vector2Int cell, Vector2 share) {
            Vector2Int step = new(share.y <= 0f ? -1 : share.y >= 1f ? 1 : 0, share.x <= 0f ? -1 : share.x >= 1f ? 1 : 0);
            bool isInside = step == Vector2Int.zero;
            bool isNeighbourOccupied = field.Cells.ValidateIndex(cell + step) && field.Cells[cell + step].IsOccupied;
            bool isSideOccupied = IsOccupiedAt(field, cell + new Vector2Int(step.x, 0)) && IsOccupiedAt(field, cell + new Vector2Int(0, step.y));
            return isInside || isNeighbourOccupied && isSideOccupied;
        }

        private static bool IsOccupiedAt(FlightField field, Vector2Int cell) =>
            field.Cells.ValidateIndex(cell) && field.Cells[cell].IsOccupied;

        // Field space: x across (matrix y), z along (matrix x); shares inside the cell, x across and y along.
        private static Vector3 CellPoint(Vector2Int cell, float acrossShare, float alongShare, float height, float cellSize) =>
            new((cell.y + acrossShare) * cellSize, height, (cell.x + alongShare) * cellSize);

        private static bool IsOccupied(FieldCell cell) =>
            cell.IsOccupied;
    }
}
