using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

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
        private const int FULL_TURN_DEGREES = 360;
        private const float CELL_CENTER = 0.5f;
        // A large prime keeps the visual sequences of neighbouring placements apart (like the scene builder).
        private const int VISUAL_SEED_STEP = 486187739;

        private readonly Dictionary<FieldRockVisual, Vector3[]> _verticesByPrefab = new();
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
                    Assert.IsFalse(rocks.Contains(center) || rocks.Contains(center + Vector3.right * halfShip) || rocks.Contains(center + Vector3.left * halfShip),
                        $"{profile.Id} seed {field.Seed}: a rock stands on the main centerline at {cell}.");
                }
            }
        }

        private FlightField Generate(FieldDifficultyProfile profile, int seed) =>
            _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, LOOP), profile));

        // The yaw-only plane rock covers its cells by its smallest horizontal reach from the pivot over every turn, and the
        // plane slab by its vertical one.
        private void AssertPlaneRockFits(FieldClusterRecipe recipe, FieldRockVisual prefab) {
            float horizontalHalf = float.PositiveInfinity;
            for (int degrees = 0; degrees < FULL_TURN_DEGREES; degrees++) {
                Bounds turned = MeshBounds(prefab, Quaternion.Euler(0f, degrees, 0f), Vector3.one);
                horizontalHalf = Mathf.Min(horizontalHalf, Mathf.Min(Mathf.Min(-turned.min.x, turned.max.x), Mathf.Min(-turned.min.z, turned.max.z)));
            }

            Assert.GreaterOrEqual(horizontalHalf + TOLERANCE, recipe.PlaneHalfExtentShare, $"{recipe.name}: {prefab.name} is narrower than the plane half-extent share.");
            Bounds bounds = MeshBounds(prefab, Quaternion.identity, Vector3.one);
            float smallestSize = (_composition.Configuration.CellSize / 2f + recipe.PlaneOverlapMeters + recipe.PlaneJitterMeters) / (recipe.PlaneHalfExtentShare * recipe.PlaneAxisScale.x);
            float halfHeight = Mathf.Min(-bounds.min.y, bounds.max.y) * smallestSize * recipe.PlaneAxisScale.x;
            Assert.GreaterOrEqual(halfHeight, _composition.Configuration.PlaneHalfThicknessMeters, $"{recipe.name}: {prefab.name} is too thin to close the flight plane slab.");
        }

        private FieldRockBoundsIndex LayOutRocks(FlightField field) {
            FieldRockBoundsIndex index = new(field.Frame.CellSize);
            List<FieldClusterRockPose> rocks = new();
            for (int i = 0; i < field.Placements.Count; i++) {
                FieldPlacement placement = field.Placements[i];
                FieldStructureDefinition structure = _composition.Catalog.GetRequired(placement.StructureId);
                FieldFootprint footprint = _composition.FootprintReader.Read(structure, placement.Rotation);
                Pose pose = _structureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, field.Frame.CellSize);
                _clusterLayout.Fill(structure.ClusterRecipe, footprint, field.Frame.CellSize, new FieldRandom(unchecked(field.Seed + i * VISUAL_SEED_STEP)), rocks);
                foreach (FieldClusterRockPose rock in rocks) {
                    Bounds bounds = MeshBounds(rock.Prefab, rock.Rotation, rock.Scale);
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
            foreach (Vector2Int cell in field.Cells.GetIndexes(IsOccupied)) {
                for (int u = 0; u <= steps; u++) {
                    for (int v = 0; v <= steps; v++)
                        AssertColumnClosed(field, rocks, cell, new Vector2(u / (float)steps, v / (float)steps), halfThickness);
                }
            }
        }

        // An edge point is checked only when the cell beyond the edge is occupied too: a free neighbour may stay open.
        private static void AssertColumnClosed(FlightField field, FieldRockBoundsIndex rocks, Vector2Int cell, Vector2 share, float halfThickness) {
            if (!IsBetweenOccupiedCells(field, cell, share))
                return;

            foreach (float height in new[] { -halfThickness, 0f, halfThickness }) {
                Vector3 point = CellPoint(cell, share.x, share.y, height, field.Frame.CellSize);
                Assert.IsTrue(rocks.Contains(point), $"{field.ProfileId} seed {field.Seed}: the flight plane is open in cell {cell} at {share} height {height}.");
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

        // The bounds of the rock's mesh turned and stretched as built, around its pivot.
        private Bounds MeshBounds(FieldRockVisual prefab, Quaternion rotation, Vector3 scale) {
            Vector3[] vertices = GetVertices(prefab);
            Matrix4x4 transform = Matrix4x4.TRS(Vector3.zero, rotation, scale);
            Bounds bounds = new(transform.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            foreach (Vector3 vertex in vertices)
                bounds.Encapsulate(transform.MultiplyPoint3x4(vertex));

            return bounds;
        }

        private Vector3[] GetVertices(FieldRockVisual prefab) {
            if (_verticesByPrefab.TryGetValue(prefab, out Vector3[] cached))
                return cached;

            List<Vector3> vertices = new();
            foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true)) {
                Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in meshFilter.sharedMesh.vertices)
                    vertices.Add(toRoot.MultiplyPoint3x4(vertex));
            }

            Vector3[] result = vertices.ToArray();
            _verticesByPrefab.Add(prefab, result);
            return result;
        }

        private static bool IsOccupied(FieldCell cell) =>
            cell.IsOccupied;
    }
}
