using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace Features.ConstellationModule.Editor.Tests {
    // Several figures share one mesh ("Show all"): each keeps the vertices it has alone, so its look is unchanged, and
    // its reveal steps follow the figures before it.
    public sealed class ConstellationMeshBuilderTests {
        private const int STEP_CHANNEL = 3;
        private const int OTHER_END_AND_STEP_UV = 1;

        private readonly List<Mesh> _meshes = new();

        private ConstellationCatalog _catalog;
        private ConstellationMeshBuilder _builder;

        [SetUp]
        public void SetUp() {
            _catalog = AddressableConfigurationLoader.Load<ConstellationCatalog>(Address.Configurations.ConstellationCatalog_Default);
            CelestialSkyFrameConfiguration frame = AddressableConfigurationLoader.Load<CelestialSkyFrameConfiguration>(Address.Configurations.CelestialSkyFrameConfiguration_LobbySky);
            ConstellationHighlightConfiguration look = AddressableConfigurationLoader.Load<ConstellationHighlightConfiguration>(Address.Configurations.ConstellationHighlightConfiguration_Default);
            _builder = new ConstellationMeshBuilder(frame, look);
        }

        [TearDown]
        public void TearDown() {
            foreach (Mesh mesh in _meshes)
                Object.DestroyImmediate(mesh);
            _meshes.Clear();
        }

        [Test]
        public void WhenAFigureIsBuiltAfterAnother_ThenItsVerticesAreTheSameAsAlone() {
            ConstellationDefinition first = _catalog.Constellations[0];
            ConstellationDefinition second = _catalog.Constellations[1];
            Mesh alone = Build(second);
            Mesh together = Build(first, second);
            int offset = Build(first).vertexCount;

            Vector3[] aloneVertices = alone.vertices;
            Vector3[] togetherVertices = together.vertices;
            List<Vector4> aloneShapes = ReadUvs(alone, 0);
            List<Vector4> togetherShapes = ReadUvs(together, 0);
            Assert.AreEqual(offset + alone.vertexCount, together.vertexCount);
            for (int i = 0; i < alone.vertexCount; i++) {
                Assert.AreEqual(aloneVertices[i], togetherVertices[offset + i]);
                Assert.AreEqual(aloneShapes[i], togetherShapes[offset + i]);
            }
        }

        [Test]
        public void WhenFiguresShareAMesh_ThenTheSecondFiguresStepsFollowTheFirstOnes() {
            ConstellationDefinition first = _catalog.Constellations[0];
            ConstellationDefinition second = _catalog.Constellations[1];

            Mesh together = Build(first, second);

            Assert.AreEqual(first.Stars.Count + second.Stars.Count - 1, GetLastStep(together, ConstellationMeshBuilder.STAR_SUBMESH));
            Assert.AreEqual(first.Lines.Count + second.Lines.Count - 1, GetLastStep(together, ConstellationMeshBuilder.LINE_SUBMESH));
        }

        [Test]
        public void WhenTheWholeCatalogIsBuilt_ThenEveryStarHasItsStep() {
            int stars = 0;
            foreach (ConstellationDefinition constellation in _catalog.Constellations)
                stars += constellation.Stars.Count;

            Mesh all = Build(_catalog.Constellations);

            Assert.AreEqual(stars, GetLastStep(all, ConstellationMeshBuilder.STAR_SUBMESH) + 1);
        }

        private Mesh Build(params ConstellationDefinition[] constellations) =>
            Build((IReadOnlyList<ConstellationDefinition>)constellations);

        private Mesh Build(IReadOnlyList<ConstellationDefinition> constellations) {
            Mesh mesh = _builder.Build(constellations);
            _meshes.Add(mesh);
            return mesh;
        }

        private static List<Vector4> ReadUvs(Mesh mesh, int channel) {
            List<Vector4> uvs = new();
            mesh.GetUVs(channel, uvs);
            return uvs;
        }

        private static int GetLastStep(Mesh mesh, int submesh) {
            List<Vector4> steps = ReadUvs(mesh, OTHER_END_AND_STEP_UV);
            int lastStep = -1;
            foreach (int index in mesh.GetIndices(submesh))
                lastStep = Mathf.Max(lastStep, Mathf.RoundToInt(steps[index][STEP_CHANNEL]));
            return lastStep;
        }
    }
}
