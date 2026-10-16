using System.Collections.Generic;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The decor comes only from the field seed (every peer and a rejoining player see the same picture): a chunk gives
    // the same rocks whatever was built before it, its rocks stay inside the chunk and the layers, and the planet
    // choice depends on the seed alone: its side of the sky follows the side weights whatever the light, it never
    // covers the course ahead and it shows its lit side.
    public sealed class FieldDecorLayoutTests {
        private const int SEEDS = 200;
        private const int SIDE_SHARE_SEEDS = 1000;
        private const int FIRST_SEED = 1;
        private const int CHUNK_REACH = 3;
        private const float TOLERANCE = 0.001f;
        private const float SIDE_SHARE_TOLERANCE = 0.05f;
        private const float ANGLE_TOLERANCE_DEGREES = 0.01f;
        // Under a sun 50 degrees down a planet at least 25 degrees above the plane cannot face it fully: past 90
        // degrees it shows a little less than half its lit side, up to this angle that is still a wide crescent.
        private const float LIT_SIDE_MAX_ANGLE_DEGREES = 100f;
        private const string PLANET_CHANCE_FIELD = "_planetChance";
        // A field 61 cells of 40 m wide: the route axis runs through the middle of its width.
        private static readonly Vector3 _courseStart = new(1220f, 0f, 0f);
        // A sun shining down at a slant, one shining straight along the course (the worst case for the course
        // exclusion), one shining across it and the game scene's sun, 50 degrees down (the worst case for the lit side
        // of a planet above), along and across the course.
        private static readonly Vector3[] _lightTravels = {
            new Vector3(0.6f, -0.3f, 0.7f).normalized,
            new Vector3(0f, -0.5f, 1f).normalized,
            new Vector3(-1f, -0.8f, 0.1f).normalized,
            new Vector3(0f, -0.766f, 0.643f).normalized,
            new Vector3(0.643f, -0.766f, 0f).normalized,
        };

        private FlightFieldDecorConfiguration _configuration;
        private FlightFieldDecorConfiguration _alwaysPlanetConfiguration;
        private FieldDecorChunkLayout _chunkLayout;
        private FieldRandomFactory _randomFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _configuration = FlightFieldEditorCompositionFactory.LoadConfiguration<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default);
            _chunkLayout = new FieldDecorChunkLayout(_configuration);
            _randomFactory = new FieldRandomFactory();
            _alwaysPlanetConfiguration = Object.Instantiate(_configuration);
            SerializedObject alwaysPlanet = new(_alwaysPlanetConfiguration);
            alwaysPlanet.FindProperty(PLANET_CHANCE_FIELD).floatValue = 1f;
            alwaysPlanet.ApplyModifiedPropertiesWithoutUndo();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() =>
            Object.DestroyImmediate(_alwaysPlanetConfiguration);

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
        public void WhenChunksAreLaidOut_ThenEveryRockStaysInsideItsChunkAndItsLayer() {
            IFieldRandom random = _randomFactory.Create(0);
            float chunkSize = _configuration.ChunkSize;
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                Vector2Int chunk = new(seed % CHUNK_REACH - 1, -seed % CHUNK_REACH);
                foreach (FieldClusterRockPose rock in LayChunk(random, seed, chunk)) {
                    Assert.That(rock.Offset.x, Is.InRange(chunk.x * chunkSize, (chunk.x + 1) * chunkSize), $"Seed {seed}: rock outside its chunk across the field.");
                    Assert.That(rock.Offset.z, Is.InRange(chunk.y * chunkSize, (chunk.y + 1) * chunkSize), $"Seed {seed}: rock outside its chunk along the field.");
                    Assert.That(Mathf.Abs(rock.Offset.y), Is.InRange(_configuration.LayerHeight.x - TOLERANCE, _configuration.LayerHeight.y + TOLERANCE),
                        $"Seed {seed}: rock outside the decor layers.");
                }
            }
        }

        [Test]
        public void WhenPlanetsArePickedOnManySeeds_ThenTheSideSharesFollowTheWeights() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _randomFactory);
            Dictionary<FlightFieldDecorConfiguration.PlanetSide, int> countsBySide = new();
            List<FieldDecorPlanet> planets = new();
            int planetCount = 0;
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                picker.Fill(seed, _courseStart, _lightTravels[seed % _lightTravels.Length], planets);
                foreach (FieldDecorPlanet planet in planets) {
                    countsBySide.TryGetValue(planet.Side, out int count);
                    countsBySide[planet.Side] = count + 1;
                    planetCount++;
                }
            }

            float totalWeight = 0f;
            foreach (FlightFieldDecorConfiguration.PlanetSide side in _alwaysPlanetConfiguration.PlanetSides)
                totalWeight += side.Weight;

            foreach (FlightFieldDecorConfiguration.PlanetSide side in _alwaysPlanetConfiguration.PlanetSides) {
                countsBySide.TryGetValue(side, out int count);
                Assert.AreEqual(side.Weight / totalWeight, (float)count / planetCount, SIDE_SHARE_TOLERANCE, $"Side {side.Name}: its share of {planetCount} planets is off its weight.");
            }
        }

        [Test]
        public void WhenPlanetsArePickedWithAnyLight_ThenNoPlanetCoversTheCourseAhead() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                foreach (Vector3 lightTravel in _lightTravels) {
                    picker.Fill(seed, _courseStart, lightTravel, planets);
                    AssertClearOfCourse(planets, seed);
                }

                picker.Fill(seed, _courseStart, planets);
                AssertClearOfCourse(planets, seed);
            }
        }

        [Test]
        public void WhenPlanetsArePickedAlongTheLight_ThenEveryPlanetStaysOnItsSideAndShowsALitCrescentAtLeast() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                foreach (Vector3 lightTravel in _lightTravels) {
                    picker.Fill(seed, _courseStart, lightTravel, planets);
                    foreach (FieldDecorPlanet planet in planets)
                        AssertPlanetOnItsLitSide(planet, lightTravel, seed);
                }
            }
        }

        [Test]
        public void WhenTheSameSeedPicksPlanetsTwice_ThenThePlanetsAreTheSame() {
            FieldDecorPlanetPicker firstPicker = new(_configuration, _randomFactory);
            FieldDecorPlanetPicker secondPicker = new(_configuration, _randomFactory);
            List<FieldDecorPlanet> first = new();
            List<FieldDecorPlanet> second = new();
            int seedsWithPlanet = 0;
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                Vector3 lightTravel = _lightTravels[seed % _lightTravels.Length];
                firstPicker.Fill(seed, _courseStart, lightTravel, first);
                secondPicker.Fill(seed + 1, _courseStart, lightTravel, second);
                secondPicker.Fill(seed, _courseStart, lightTravel, second);
                AssertSamePlanets(first, second, seed);
                firstPicker.Fill(seed, _courseStart, first);
                secondPicker.Fill(seed + 1, _courseStart, second);
                secondPicker.Fill(seed, _courseStart, second);
                AssertSamePlanets(first, second, seed);
                seedsWithPlanet += first.Count > 0 ? 1 : 0;
            }

            Assert.That(seedsWithPlanet, Is.InRange(1, SEEDS - 1), "The planet chance gives every flight the same sky.");
        }

        private void AssertSamePlanets(List<FieldDecorPlanet> first, List<FieldDecorPlanet> second, int seed) {
            Assert.AreEqual(first.Count, second.Count, $"Seed {seed}: planet count differs.");
            Assert.LessOrEqual(first.Count, _configuration.MaxPlanetsPerFlight, $"Seed {seed}: too many planets.");
            for (int i = 0; i < first.Count; i++) {
                Assert.AreSame(first[i].Prefab, second[i].Prefab, $"Seed {seed}: planet prefab differs.");
                Assert.AreSame(first[i].Side, second[i].Side, $"Seed {seed}: planet side differs.");
                Assert.Less(Vector3.Distance(first[i].Position, second[i].Position), TOLERANCE, $"Seed {seed}: planet position differs.");
                Assert.AreEqual(first[i].Diameter, second[i].Diameter, TOLERANCE, $"Seed {seed}: planet size differs.");
            }
        }

        private void AssertClearOfCourse(List<FieldDecorPlanet> planets, int seed) {
            foreach (FieldDecorPlanet planet in planets) {
                Assert.GreaterOrEqual(Vector3.Angle(planet.Position - _courseStart, Vector3.forward), _configuration.PlanetMinAngleFromCourse - ANGLE_TOLERANCE_DEGREES,
                    $"Seed {seed}: the {planet.Side.Name} planet stands in front of station B.");
            }
        }

        private void AssertPlanetOnItsLitSide(FieldDecorPlanet planet, Vector3 lightTravel, int seed) {
            Vector2 elevation = planet.Side.ElevationDegrees;
            if (elevation.x > 0f && !planet.Side.IsMirroredBelowPlane)
                Assert.Greater(planet.Position.y, 0f, $"Seed {seed}: the {planet.Side.Name} planet stands below the flight plane.");

            if (elevation.y < 0f)
                Assert.Less(planet.Position.y, 0f, $"Seed {seed}: the {planet.Side.Name} planet stands above the flight plane.");

            Vector3 besideRoute = planet.Position - new Vector3(_courseStart.x, 0f, planet.Position.z);
            Assert.LessOrEqual(besideRoute.magnitude, _configuration.PlanetDistance.y + TOLERANCE, $"Seed {seed}: the planet is too far from the route.");
            Assert.Less(Vector3.Angle(planet.Position - _courseStart, lightTravel), LIT_SIDE_MAX_ANGLE_DEGREES,
                $"Seed {seed}, light {lightTravel}: the {planet.Side.Name} planet shows little more than its night side.");
        }

        private List<FieldClusterRockPose> LayChunk(IFieldRandom random, int seed, Vector2Int chunk) {
            List<FieldClusterRockPose> rocks = new();
            _chunkLayout.Fill(seed, chunk, random, rocks);
            return rocks;
        }

        private static void AssertSameRock(FieldClusterRockPose expected, FieldClusterRockPose actual, int seed) {
            Assert.AreSame(expected.Prefab, actual.Prefab, $"Seed {seed}: rock prefab differs.");
            Assert.AreSame(expected.Material, actual.Material, $"Seed {seed}: rock material differs.");
            Assert.Less(Vector3.Distance(expected.Offset, actual.Offset), TOLERANCE, $"Seed {seed}: rock position differs.");
            Assert.Less(Quaternion.Angle(expected.Rotation, actual.Rotation), TOLERANCE, $"Seed {seed}: rock turn differs.");
            Assert.Less(Vector3.Distance(expected.Scale, actual.Scale), TOLERANCE, $"Seed {seed}: rock scale differs.");
        }
    }
}
