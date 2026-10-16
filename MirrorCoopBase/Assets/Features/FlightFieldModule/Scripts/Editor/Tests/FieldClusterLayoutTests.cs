using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The rocks of a cluster come only from the seed (every peer builds the same field), the first rock (the giant)
    // crosses the flight plane, every rock but the giants stays inside the footprint (the client does not know the
    // corridor cells) and within the recipe's height, giants stay on the plane and stick out of the footprint by less
    // than a cell, and the game's recipes are complete visual-only data whose meshes fit the layout's reach.
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
        // Outcrops stand up to this share of the giant's half height (see FieldClusterLayout).
        private const float OUTCROP_HIGHEST_SHARE = 0.95f;
        // A giant may stick out of its footprint by its protrusion, the yaw jitter's swing and its mesh's uneven sides:
        // less than a cell, so a corridor's centerline (a cell and a half from any giant's footprint) keeps the ship's
        // room (FieldPlaneClosedTests checks it on real fields).
        private const float PLANE_SPILL_LIMIT = CELL_SIZE;

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
                    float crossing = first.IsPlaneCover ? first.Scale.y * structure.ClusterRecipe.PlaneHalfHeightShare
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

        private void AssertRocksStayInside(FieldStructureDefinition structure, int seed, float acrossLimit, float alongLimit) {
            List<FieldClusterRockPose> rocks = Lay(structure, seed);
            Assert.GreaterOrEqual(rocks.Count, structure.ClusterRecipe.SatelliteCount.x + 1, $"{structure.name} seed {seed}: too few satellites.");
            float highest = Mathf.Max(structure.ClusterRecipe.HeightSpreadCells * CELL_SIZE, HighestOutcrop(structure.ClusterRecipe));
            foreach (FieldClusterRockPose rock in rocks) {
                if (rock.IsPlaneCover) {
                    AssertPlaneRockNearFootprint(structure, seed, rock, acrossLimit, alongLimit);
                    continue;
                }

                float reach = Mathf.Max(rock.Scale.x, Mathf.Max(rock.Scale.y, rock.Scale.z)) * ROCK_REACH;
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.x) + reach, acrossLimit, $"{structure.name} seed {seed}: a rock reaches too far across the field.");
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.z) + reach, alongLimit, $"{structure.name} seed {seed}: a rock reaches too far along the field.");
                Assert.LessOrEqual(Mathf.Abs(rock.Offset.y), highest + TOLERANCE, $"{structure.name} seed {seed}: a rock is too far from the plane.");
            }
        }

        // Outcrops stand up to the top of the highest giant the recipe can make.
        private static float HighestOutcrop(FieldClusterRecipe recipe) =>
            recipe.HasPlaneCover ? recipe.PlaneHalfHeightMeters.y * (1f + recipe.PlaneHeightVariation) * OUTCROP_HIGHEST_SHARE : 0f;

        // The real mesh bounds of a giant, turned and stretched as built.
        private void AssertPlaneRockNearFootprint(FieldStructureDefinition structure, int seed, FieldClusterRockPose rock, float acrossLimit, float alongLimit) {
            Bounds bounds = _meshBounds.Get(rock.Prefab, rock.Rotation, rock.Scale);
            Assert.LessOrEqual(Mathf.Abs(rock.Offset.y), structure.ClusterRecipe.PlaneHeightJitterMeters + TOLERANCE, $"{structure.name} seed {seed}: a giant left the flight plane.");
            Assert.LessOrEqual(Mathf.Abs(rock.Offset.x) + bounds.extents.x + Mathf.Abs(bounds.center.x), acrossLimit + PLANE_SPILL_LIMIT,
                $"{structure.name} seed {seed}: a giant reaches too far past the footprint.");
            Assert.LessOrEqual(Mathf.Abs(rock.Offset.z) + bounds.extents.z + Mathf.Abs(bounds.center.z), alongLimit + PLANE_SPILL_LIMIT,
                $"{structure.name} seed {seed}: a giant reaches too far past the footprint.");
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
