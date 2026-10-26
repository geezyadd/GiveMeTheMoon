using System;
using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Scene;
using NUnit.Framework;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The volume of big rocks above and below the plane comes only from the field seed (every peer and a rejoining
    // player see the same rocks): a chunk gives the same rocks whatever was built before it; every rock stays inside
    // a band and clear of the flight plane; two rocks never fuse, also across chunks; no rock stands below the plane
    // near the route's ends; and every profile's volume holds rocks.
    public sealed class FieldVolumeLayoutTests {
        private const int SEEDS = 60;
        private const int FIRST_SEED = 1;
        private const int CHUNK_REACH = 2;
        private const float TOLERANCE = 0.01f;
        private const float CELL_SIZE = 40f;
        private const float FAR_AWAY = 1e6f;

        private readonly FieldRandomFactory _randomFactory = new();
        private readonly FieldTestVolumeSourceReader _sourceReader = new();

        private FlightFieldConfiguration _configuration;
        private FieldDecorChunkLayout _chunkLayout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _configuration = AddressableConfigurationLoader.Load<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default);
            FlightFieldDecorConfiguration decor = AddressableConfigurationLoader.Load<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default);
            FieldVolumeCandidates candidates = new(new FieldRidgeNoise(new FieldValueNoise()), _randomFactory);
            _chunkLayout = new FieldDecorChunkLayout(decor, candidates, _sourceReader);
            UseSource(_configuration.DifficultyProfiles[0], Vector2.one * FAR_AWAY);
        }

        [Test]
        public void WhenAChunkIsLaidOutAfterOtherChunks_ThenItsRocksAreTheSameAsWhenLaidOutAlone() {
            IFieldRandom sharedRandom = _randomFactory.Create(0);
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                Vector2Int chunk = new(seed % CHUNK_REACH - 1, seed / CHUNK_REACH);
                List<FieldClusterRockPose> alone = LayChunk(_randomFactory.Create(0), seed, chunk);
                LayChunk(sharedRandom, seed, chunk + Vector2Int.one);
                List<FieldClusterRockPose> afterOthers = LayChunk(sharedRandom, seed, chunk);
                Assert.AreEqual(alone.Count, afterOthers.Count, $"Seed {seed}, chunk {chunk}: rock count differs.");
                for (int i = 0; i < alone.Count; i++)
                    AssertSameRock(alone[i], afterOthers[i], seed);
            }
        }

        [Test]
        public void WhenChunksAreLaidOut_ThenEveryRockStaysInABandAndClearOfTheFlightPlane() {
            IFieldRandom random = _randomFactory.Create(0);
            FieldVolumeConfiguration volume = _sourceReader.Source.Volume;
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                foreach (FieldClusterRockPose rock in LayChunk(random, seed, new Vector2Int(seed % CHUNK_REACH, -seed % CHUNK_REACH))) {
                    float radius = Reach(rock);
                    Assert.GreaterOrEqual(Mathf.Abs(rock.Offset.y) - radius, volume.PlaneClearanceMeters - TOLERANCE, $"Seed {seed}: a rock comes too close to the flight plane.");
                    Assert.IsTrue(IsInABand(volume, Mathf.Abs(rock.Offset.y), radius), $"Seed {seed}: a rock at {rock.Offset.y} m stands outside every band.");
                }
            }
        }

        [Test]
        public void WhenNeighbouringChunksAreLaidOut_ThenNoTwoRocksFuse() {
            float gap = SmallestGap(_sourceReader.Source.Volume);
            for (int seed = FIRST_SEED; seed <= SEEDS / CHUNK_REACH; seed++) {
                List<FieldClusterRockPose> rocks = new();
                for (int x = 0; x < CHUNK_REACH; x++) {
                    for (int z = 0; z < CHUNK_REACH; z++)
                        rocks.AddRange(LayChunk(_randomFactory.Create(0), seed, new Vector2Int(x, z)));
                }

                AssertApart(rocks, gap, seed);
            }
        }

        [Test]
        public void WhenTheRouteEndsAreNear_ThenNoRockStandsBelowThePlaneThere() {
            UseSource(_configuration.DifficultyProfiles[0], Vector2.zero);
            try {
                float clearance = _sourceReader.Source.Volume.StationClearanceMeters;
                for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                    foreach (FieldClusterRockPose rock in LayChunk(_randomFactory.Create(0), seed, Vector2Int.zero)) {
                        bool isBelowNearStart = rock.Offset.y < 0f && new Vector2(rock.Offset.x, rock.Offset.z).magnitude < clearance;
                        Assert.IsFalse(isBelowNearStart, $"Seed {seed}: a rock stands below the plane where the ship climbs from the station.");
                    }
                }
            } finally {
                UseSource(_configuration.DifficultyProfiles[0], Vector2.one * FAR_AWAY);
            }
        }

        [Test]
        public void WhenEveryProfileIsLaidOut_ThenItsVolumeHoldsRocks() {
            try {
                foreach (FieldDifficultyProfile profile in _configuration.DifficultyProfiles) {
                    UseSource(profile, Vector2.one * FAR_AWAY);
                    int rocks = 0;
                    for (int seed = FIRST_SEED; seed <= SEEDS; seed++)
                        rocks += LayChunk(_randomFactory.Create(0), seed, Vector2Int.zero).Count;

                    Assert.Greater(rocks, SEEDS, $"{profile.Id}: the volume above and below the plane is almost empty.");
                }
            } finally {
                UseSource(_configuration.DifficultyProfiles[0], Vector2.one * FAR_AWAY);
            }
        }

        private void UseSource(FieldDifficultyProfile profile, Vector2 routeEnds) =>
            _sourceReader.Source = new FieldVolumeSource(profile, profile.HasOwnVolume ? profile.Volume : _configuration.Volume, 0f, CELL_SIZE, routeEnds, routeEnds,
                Array.Empty<FieldVolumeGiant>());

        private List<FieldClusterRockPose> LayChunk(IFieldRandom random, int seed, Vector2Int chunk) {
            List<FieldClusterRockPose> rocks = new();
            _chunkLayout.Fill(seed, chunk, random, rocks);
            return rocks;
        }

        private static void AssertApart(List<FieldClusterRockPose> rocks, float gap, int seed) {
            for (int i = 0; i < rocks.Count; i++) {
                for (int j = i + 1; j < rocks.Count; j++) {
                    float clearance = Vector3.Distance(rocks[i].Offset, rocks[j].Offset) - Reach(rocks[i]) - Reach(rocks[j]);
                    Assert.GreaterOrEqual(clearance, gap - TOLERANCE, $"Seed {seed}: two volume rocks fuse at {rocks[i].Offset} and {rocks[j].Offset}.");
                }
            }
        }

        private static bool IsInABand(FieldVolumeConfiguration volume, float height, float radius) {
            foreach (FieldVolumeConfiguration.Band band in volume.Bands) {
                if (height - radius >= band.HeightMeters.x - TOLERANCE && height + radius <= band.HeightMeters.y + TOLERANCE)
                    return true;
            }

            return false;
        }

        private static float SmallestGap(FieldVolumeConfiguration volume) {
            float smallest = float.PositiveInfinity;
            foreach (FieldVolumeConfiguration.Band band in volume.Bands)
                smallest = Mathf.Min(smallest, band.GapMeters);

            return smallest;
        }

        // However the rock is turned, its mesh stays within this distance of its middle.
        private static float Reach(FieldClusterRockPose rock) =>
            Mathf.Max(rock.Scale.x, Mathf.Max(rock.Scale.y, rock.Scale.z)) * FieldRockVisual.MESH_REACH;

        private static void AssertSameRock(FieldClusterRockPose expected, FieldClusterRockPose actual, int seed) {
            Assert.AreSame(expected.Prefab, actual.Prefab, $"Seed {seed}: rock prefab differs.");
            Assert.AreSame(expected.Material, actual.Material, $"Seed {seed}: rock material differs.");
            Assert.Less(Vector3.Distance(expected.Offset, actual.Offset), TOLERANCE, $"Seed {seed}: rock position differs.");
            Assert.Less(Quaternion.Angle(expected.Rotation, actual.Rotation), TOLERANCE, $"Seed {seed}: rock turn differs.");
            Assert.Less(Vector3.Distance(expected.Scale, actual.Scale), TOLERANCE, $"Seed {seed}: rock scale differs.");
        }
    }
}
