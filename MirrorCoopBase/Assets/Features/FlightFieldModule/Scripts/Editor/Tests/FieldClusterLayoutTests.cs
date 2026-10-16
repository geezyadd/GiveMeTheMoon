using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The rocks of a cluster come only from the seed (every peer builds the same field), the main rock crosses the
    // flight plane, every rock stays inside the footprint (the client does not know the corridor cells) and within the
    // recipe's height, and the game's recipes are complete visual-only data whose meshes fit the layout's reach.
    public sealed class FieldClusterLayoutTests {
        private const float CELL_SIZE = 40f;
        private const int SEEDS = 200;
        private const int FIRST_SEED = 1;
        private const float TOLERANCE = 0.001f;
        // The main rock's height offset stays below this share of its smallest axis, so its surface crosses the plane.
        private const float MAIN_CROSSING_SHARE = 0.15f;
        // The farthest vertex of a rock prefab from its pivot, as a share of its size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;
        private const float MAX_HEIGHT_CELLS = 0.75f;
        private const float HALF = 0.5f;

        private FlightFieldEditorComposition _composition;
        private FieldClusterLayout _layout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _layout = new FieldClusterLayout();
        }

        [Test]
        public void WhenTheSameSeedIsLaidOutTwice_ThenTheRocksAreTheSame() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                    List<FieldClusterRockPose> first = Lay(structure, seed);
                    List<FieldClusterRockPose> second = Lay(structure, seed);

                    Assert.AreEqual(first.Count, second.Count, $"{structure.name} seed {seed}: another rock count.");
                    for (int i = 0; i < first.Count; i++)
                        AssertSameRock(first[i], second[i], $"{structure.name} seed {seed} rock {i}");
                }
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
        public void WhenAClusterIsLaidOut_ThenTheMainRockCrossesTheFlightPlane() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                    FieldClusterRockPose main = Lay(structure, seed)[0];
                    float smallestAxis = Mathf.Min(main.Scale.x, Mathf.Min(main.Scale.y, main.Scale.z));

                    Assert.Less(Mathf.Abs(main.Offset.y), smallestAxis * MAIN_CROSSING_SHARE, $"{structure.name} seed {seed}: the main rock may miss the flight plane.");
                }
            }
        }

        [Test]
        public void WhenAClusterIsLaidOut_ThenEveryRockStaysInsideTheFootprintAndNearThePlane() {
            foreach (FieldStructureDefinition structure in GetClusterStructures()) {
                FieldFootprint footprint = _composition.FootprintReader.Read(structure, FieldRotation.None);
                float acrossLimit = footprint.Height * HALF * CELL_SIZE + TOLERANCE;
                float alongLimit = footprint.Width * HALF * CELL_SIZE + TOLERANCE;
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                    List<FieldClusterRockPose> rocks = Lay(structure, seed);
                    Assert.GreaterOrEqual(rocks.Count, structure.ClusterRecipe.SatelliteCount.x + 1, $"{structure.name} seed {seed}: too few satellites.");
                    foreach (FieldClusterRockPose rock in rocks) {
                        float reach = Mathf.Max(rock.Scale.x, Mathf.Max(rock.Scale.y, rock.Scale.z)) * ROCK_REACH;
                        Assert.LessOrEqual(Mathf.Abs(rock.Offset.x) + reach, acrossLimit, $"{structure.name} seed {seed}: a rock reaches too far across the field.");
                        Assert.LessOrEqual(Mathf.Abs(rock.Offset.z) + reach, alongLimit, $"{structure.name} seed {seed}: a rock reaches too far along the field.");
                        Assert.LessOrEqual(Mathf.Abs(rock.Offset.y), MAX_HEIGHT_CELLS * CELL_SIZE + TOLERANCE, $"{structure.name} seed {seed}: a rock is too far from the plane.");
                    }
                }
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

        private static void AssertSameRock(FieldClusterRockPose expected, FieldClusterRockPose actual, string what) {
            Assert.AreSame(expected.Prefab, actual.Prefab, $"{what}: another prefab.");
            Assert.AreSame(expected.Material, actual.Material, $"{what}: another material.");
            Assert.Less((expected.Offset - actual.Offset).magnitude, TOLERANCE, $"{what}: another position.");
            Assert.Less(Quaternion.Angle(expected.Rotation, actual.Rotation), TOLERANCE, $"{what}: another rotation.");
            Assert.Less((expected.Scale - actual.Scale).magnitude, TOLERANCE, $"{what}: another scale.");
        }
    }
}
