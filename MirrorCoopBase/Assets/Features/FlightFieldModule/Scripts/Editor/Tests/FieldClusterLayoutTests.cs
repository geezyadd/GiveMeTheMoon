using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The rocks of a cluster come only from the seed (every peer builds the same field), the first rock crosses the
    // flight plane, every rock stays within the footprint (a dense field: within its spill, because the client does
    // not know the corridor cells) and within the recipe's height, and the game's recipes are complete visual-only
    // data whose meshes fit the layout's reach.
    public sealed class FieldClusterLayoutTests {
        private const float CELL_SIZE = 40f;
        private const int SEEDS = 200;
        private const int FIRST_SEED = 1;
        private const float TOLERANCE = 0.001f;
        // The main rock's height offset stays below this share of its smallest axis, so its surface crosses the plane.
        private const float MAIN_CROSSING_SHARE = 0.15f;
        // The farthest vertex of a rock prefab from its pivot, as a share of its size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;
        private const float HALF = 0.5f;

        private readonly FieldRockMeshBounds _meshBounds = new();

        private FlightFieldEditorComposition _composition;
        private FieldClusterLayout _layout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _layout = new FieldClusterLayout(new FieldPlaneCoverLayout());
        }

        [Test]
        public void WhenTheSameSeedIsLaidOutTwice_ThenTheRocksAreTheSame() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++)
                    AssertSameCluster(structure, seed);
            }
        }

        [Test]
        public void WhenTheSeedChanges_ThenTheClusterChanges() {
            FieldStructureDefinition structure = GetClusterStructures().First();

            List<FieldClusterRockPose> first = Lay(structure, FIRST_SEED);
            List<FieldClusterRockPose> second = Lay(structure, FIRST_SEED + 1);

            Assert.Greater((first[0].Rotation.eulerAngles - second[0].Rotation.eulerAngles).magnitude, TOLERANCE, $"{structure.name}: two seeds gave the same main rock.");
        }

        [Test]
        public void WhenAClusterIsLaidOut_ThenItsFirstRockCrossesTheFlightPlane() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                    FieldClusterRockPose first = Lay(structure, seed)[0];
                    float crossing = first.IsPlaneCover ? first.Scale.y * structure.ClusterRecipe.PlaneHalfExtentShare
                        : Mathf.Min(first.Scale.x, Mathf.Min(first.Scale.y, first.Scale.z)) * MAIN_CROSSING_SHARE;

                    Assert.Less(Mathf.Abs(first.Offset.y), crossing, $"{structure.name} seed {seed}: the first rock may miss the flight plane.");
                }
            }
        }

        [Test]
        public void WhenAClusterIsLaidOut_ThenEveryRockStaysInsideTheFootprintAndNearThePlane() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                FieldFootprint footprint = _composition.FootprintReader.Read(structure, FieldRotation.None);
                float acrossLimit = footprint.Height * HALF * CELL_SIZE + TOLERANCE;
                float alongLimit = footprint.Width * HALF * CELL_SIZE + TOLERANCE;
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++)
                    AssertRocksStayInside(structure, seed, acrossLimit, alongLimit);
            }
        }

        [Test]
        public void WhenCatalogIsRead_ThenEveryLocalStructureHasACompleteRecipeOfColliderFreeRocks() {
            foreach (FieldStructureDefinition structure in _composition.Catalog.Structures.Where(structure => !structure.IsNetworked)) {
                Assert.IsTrue(structure.HasClusterRecipe, $"{structure.name} has no cluster recipe: it would be an invisible trigger.");
                FieldClusterRecipe recipe = structure.ClusterRecipe;
                Assert.IsNotEmpty(recipe.Rocks, $"{recipe.name} has no rocks.");
                Assert.IsNotEmpty(recipe.Materials, $"{recipe.name} has no materials.");
                Assert.IsTrue(recipe.Materials.All(material => material != null), $"{recipe.name} has an empty material slot.");
                foreach (FieldClusterRockOption option in recipe.Rocks) {
                    Assert.IsNotNull(option.Prefab, $"{recipe.name} has an empty rock slot.");
                    Assert.IsEmpty(option.Prefab.GetComponentsInChildren<Collider>(true), $"{option.Prefab.name} has a collider: visual rocks must not collide.");
                    Assert.LessOrEqual(GetFarthestVertex(option.Prefab), ROCK_REACH + TOLERANCE, $"{option.Prefab.name} reaches farther than the layout allows: normalise its mesh to about 1 m.");
                }
            }
        }

        // In the prefab root's space, so a rock of size 1 reaches this far from its pivot however it is turned.
        private static float GetFarthestVertex(FieldRockVisual rock) {
            float farthest = 0f;
            foreach (MeshFilter meshFilter in rock.GetComponentsInChildren<MeshFilter>(true)) {
                Matrix4x4 toRoot = rock.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in meshFilter.sharedMesh.vertices)
                    farthest = Mathf.Max(farthest, toRoot.MultiplyPoint3x4(vertex).magnitude);
            }

            return farthest;
        }

        private IEnumerable<FieldStructureDefinition> GetClusterStructures() =>
            _composition.Catalog.Structures.Where(structure => structure.HasClusterRecipe);

        private List<FieldClusterRockPose> Lay(FieldStructureDefinition structure, int seed) {
            List<FieldClusterRockPose> rocks = new();
            FieldFootprint footprint = _composition.FootprintReader.Read(structure, FieldRotation.None);
            _layout.Fill(structure.ClusterRecipe, footprint, CELL_SIZE, new FieldRandom(seed), rocks);
            return rocks;
        }

        private void AssertSameCluster(FieldStructureDefinition structure, int seed) {
            List<FieldClusterRockPose> first = Lay(structure, seed);
            List<FieldClusterRockPose> second = Lay(structure, seed);

            Assert.AreEqual(first.Count, second.Count, $"{structure.name} seed {seed}: another rock count.");
            for (int i = 0; i < first.Count; i++)
                AssertSameRock(first[i], second[i], $"{structure.name} seed {seed} rock {i}");
        }

        // A dense-field recipe's rocks may reach past the footprint by its spill (extra rocks by their own radius,
        // which stays within the spill); other recipes keep every rock inside.
        private void AssertRocksStayInside(FieldStructureDefinition structure, int seed, float acrossLimit, float alongLimit) {
            FieldClusterRecipe recipe = structure.ClusterRecipe;
            List<FieldClusterRockPose> rocks = Lay(structure, seed);
            float spill = recipe.HasPlaneCover ? recipe.PlaneMaxSpillMeters : 0f;
            float highest = recipe.HasPlaneCover ? recipe.ExtraHeightMeters.y : recipe.HeightSpreadCells * CELL_SIZE;
            Assert.IsNotEmpty(rocks, $"{structure.name} seed {seed}: no rock.");
            foreach (FieldClusterRockPose rock in rocks) {
                Bounds bounds = _meshBounds.Get(rock.Prefab, rock.Rotation, rock.Scale);
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.x) + bounds.extents.x + Mathf.Abs(bounds.center.x), acrossLimit + spill,
                    $"{structure.name} seed {seed}: a rock reaches too far across the field.");
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.z) + bounds.extents.z + Mathf.Abs(bounds.center.z), alongLimit + spill,
                    $"{structure.name} seed {seed}: a rock reaches too far along the field.");
                float heightLimit = rock.IsPlaneCover ? recipe.PlaneHeightJitterMeters : highest;
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.y), heightLimit + TOLERANCE, $"{structure.name} seed {seed}: a rock is too far from the plane.");
            }
        }

        private static void AssertSameRock(FieldClusterRockPose expected, FieldClusterRockPose actual, string what) {
            Assert.AreSame(expected.Prefab, actual.Prefab, $"{what}: another prefab.");
            Assert.AreSame(expected.Material, actual.Material, $"{what}: another material.");
            Assert.Less((expected.Offset - actual.Offset).magnitude, TOLERANCE, $"{what}: another position.");
            Assert.Less(Quaternion.Angle(expected.Rotation, actual.Rotation), TOLERANCE, $"{what}: another rotation.");
            Assert.Less((expected.Scale - actual.Scale).magnitude, TOLERANCE, $"{what}: another scale.");
        }
    }
}
