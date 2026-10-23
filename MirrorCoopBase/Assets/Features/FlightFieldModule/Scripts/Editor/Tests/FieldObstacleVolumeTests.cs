using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The volume above and below an obstacle field, laid out over the whole field of a few seeds with the field's own
    // giants: no volume rock comes into the clearance band around the flight plane or into a giant, two rocks never
    // fuse (also of different bands), and every height slab up to the top holds rocks (no empty band).
    public sealed class FieldObstacleVolumeTests {
        private const int SEEDS = 3;
        private const int FIRST_SEED = 1;
        private const float TOLERANCE = 0.01f;
        private const float SLAB_METERS = 100f;

        private readonly FieldRandomFactory _randomFactory = new();
        private readonly FieldTestVolumeSourceReader _sourceReader = new();
        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;
        private FlightFieldDecorConfiguration _decor;
        private FieldDecorChunkLayout _chunkLayout;
        private FieldStructureLayout _structureLayout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _structureLayout = new FieldStructureLayout(_composition.FootprintReader);
            _decor = AddressableConfigurationLoader.Load<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default);
            FieldVolumeCandidates candidates = new(new FieldRidgeNoise(new FieldValueNoise()), _randomFactory);
            _chunkLayout = new FieldDecorChunkLayout(_decor, candidates, _sourceReader);
        }

        [Test]
        public void WhenTheVolumeOfAnObstacleFieldIsLaidOut_ThenNoRockComesIntoTheClearanceBand() {
            foreach ((FlightField field, FieldVolumeSource source, List<FieldClusterRockPose> rocks) in LayOutFields()) {
                foreach (FieldClusterRockPose rock in rocks)
                    Assert.GreaterOrEqual(Mathf.Abs(rock.Offset.y) - Reach(rock), source.Volume.PlaneClearanceMeters - TOLERANCE, $"{Name(field)}: a volume rock at {rock.Offset} comes into the clearance band.");
            }
        }

        [Test]
        public void WhenTheVolumeOfAnObstacleFieldIsLaidOut_ThenNoRockComesIntoAGiant() {
            foreach ((FlightField field, FieldVolumeSource source, List<FieldClusterRockPose> rocks) in LayOutFields()) {
                foreach (FieldClusterRockPose rock in rocks)
                    AssertClearOfGiants(field, source, rock);
            }
        }

        [Test]
        public void WhenTheVolumeOfAnObstacleFieldIsLaidOut_ThenNoTwoRocksFuse() {
            foreach ((FlightField field, FieldVolumeSource _, List<FieldClusterRockPose> rocks) in LayOutFields()) {
                for (int i = 0; i < rocks.Count; i++)
                    AssertApartFromLater(field, rocks, i);
            }
        }

        [Test]
        public void WhenTheVolumeOfAnObstacleFieldIsLaidOut_ThenEveryHeightSlabHoldsRocks() {
            foreach ((FlightField field, FieldVolumeSource source, List<FieldClusterRockPose> rocks) in LayOutFields()) {
                float top = FieldVolumeHeights.FindTop(source.Volume);
                int[] counts = new int[Mathf.CeilToInt(top / SLAB_METERS)];
                foreach (FieldClusterRockPose rock in rocks)
                    counts[Mathf.Min(counts.Length - 1, Mathf.FloorToInt(Mathf.Abs(rock.Offset.y) / SLAB_METERS))]++;

                for (int slab = 0; slab < counts.Length; slab++)
                    Assert.Greater(counts[slab], 0, $"{Name(field)}: no volume rock between {slab * SLAB_METERS} and {(slab + 1) * SLAB_METERS} m from the plane.");
            }
        }

        private IEnumerable<(FlightField field, FieldVolumeSource source, List<FieldClusterRockPose> rocks)> LayOutFields() {
            foreach (FieldDifficultyProfile profile in FieldTestProfiles.Obstacles(_composition.Configuration)) {
                for (int seed = FIRST_SEED; seed < FIRST_SEED + SEEDS; seed++) {
                    FlightField field = _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, seed, 0), profile));
                    FieldVolumeSource source = CreateSource(field, profile);
                    _sourceReader.Source = source;
                    yield return (field, source, LayOutField(field));
                }
            }
        }

        private List<FieldClusterRockPose> LayOutField(FlightField field) {
            List<FieldClusterRockPose> all = new();
            List<FieldClusterRockPose> chunkRocks = new();
            float chunkSize = _decor.ChunkSize;
            Vector2Int chunks = new(Mathf.CeilToInt(field.Frame.WidthCells * field.Frame.CellSize / chunkSize), Mathf.CeilToInt(field.Frame.LengthCells * field.Frame.CellSize / chunkSize));
            IFieldRandom random = _randomFactory.Create(0);
            for (int x = 0; x < chunks.x; x++) {
                for (int z = 0; z < chunks.y; z++) {
                    _chunkLayout.Fill(field.Seed, new Vector2Int(x, z), random, chunkRocks);
                    all.AddRange(chunkRocks);
                }
            }

            return all;
        }

        // As a peer reads it from the live field: the giants' spheres from their footprints and recipes.
        private FieldVolumeSource CreateSource(FlightField field, FieldDifficultyProfile profile) {
            float cellSize = field.Frame.CellSize;
            List<FieldVolumeGiant> giants = new();
            foreach (FieldPlacement placement in field.Placements) {
                FieldStructureDefinition structure = _composition.Catalog.GetRequired(placement.StructureId);
                FieldFootprint footprint = _composition.FootprintReader.Read(structure, placement.Rotation);
                Pose pose = _structureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, cellSize);
                giants.Add(FieldVolumeGiant.CreateFromPlacement(structure, footprint, pose, cellSize));
            }

            IReadOnlyList<Vector2Int> centerline = field.Routes[0].Centerline;
            return new FieldVolumeSource(profile, profile.Volume, field.Difficulty, cellSize, field.Frame.CellToPlane(centerline[0]), field.Frame.CellToPlane(centerline[^1]), giants);
        }

        private static void AssertClearOfGiants(FlightField field, FieldVolumeSource source, FieldClusterRockPose rock) {
            foreach (FieldVolumeGiant giant in source.Giants) {
                float clearance = Vector3.Distance(rock.Offset, new Vector3(giant.Center.x, 0f, giant.Center.y)) - giant.Radius - Reach(rock);
                if (clearance < -TOLERANCE)
                    Assert.Fail($"{Name(field)}: a volume rock at {rock.Offset} comes into a giant.");
            }
        }

        private static void AssertApartFromLater(FlightField field, List<FieldClusterRockPose> rocks, int index) {
            for (int j = index + 1; j < rocks.Count; j++) {
                float clearance = Vector3.Distance(rocks[index].Offset, rocks[j].Offset) - Reach(rocks[index]) - Reach(rocks[j]);
                if (clearance < -TOLERANCE)
                    Assert.Fail($"{Name(field)}: two volume rocks fuse at {rocks[index].Offset} and {rocks[j].Offset}.");
            }
        }

        // However the rock is turned, its mesh stays within this distance of its middle.
        private static float Reach(FieldClusterRockPose rock) =>
            Mathf.Max(rock.Scale.x, Mathf.Max(rock.Scale.y, rock.Scale.z)) * FieldRockVisual.MESH_REACH;

        private static string Name(FlightField field) =>
            $"{field.ProfileId} seed {field.Seed}";
    }
}
