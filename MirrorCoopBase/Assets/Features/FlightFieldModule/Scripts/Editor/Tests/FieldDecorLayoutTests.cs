using System.Collections.Generic;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The planet of the decor comes only from the field seed (every peer and a rejoining player see the same
    // picture): its side of the sky follows the side weights whatever the light, it never covers the course ahead,
    // keeps clear of the field, the volume rock bands and the stations, and shows its lit side. The volume rocks of
    // the decor chunks: FieldVolumeLayoutTests.
    public sealed class FieldDecorLayoutTests {
        private const int SEEDS = 200;
        private const int SIDE_SHARE_SEEDS = 1000;
        private const int FIRST_SEED = 1;
        private const float TOLERANCE = 0.001f;
        private const float DISTANCE_TOLERANCE = 0.05f;
        private const float SIDE_SHARE_TOLERANCE = 0.05f;
        private const float ANGLE_TOLERANCE_DEGREES = 0.01f;
        // Under a sun 50 degrees down a planet at least 25 degrees above the plane cannot face it fully: past 90
        // degrees it shows a little less than half its lit side, up to this angle that is still a wide crescent.
        private const float LIT_SIDE_MAX_ANGLE_DEGREES = 100f;
        private const string PLANET_CHANCE_FIELD = "_planetChance";
        private const float HALF_PLANET_CHANCE = 0.5f;
        // A field 61 cells of 40 m wide: the route axis runs through the middle of its width.
        private const float FIELD_HALF_WIDTH = 1220f;
        // A field of about 6 km: both station pads stand on the route axis inside it.
        private const float FIELD_LENGTH = 6000f;
        private const float MIN_SHOWN_PLANET_SHARE = 0.9f;
        private static readonly Vector3 _courseStart = new(FIELD_HALF_WIDTH, 0f, 0f);
        // The default field and fields widened for a cruise start up to about 2 km off the axis (a takeoff turned
        // up to 85 degrees from the course).
        private static readonly float[] _fieldHalfWidths = { FIELD_HALF_WIDTH, 1620f, 2020f, 2420f };
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
        // The game shows a planet on every flight; the seed check needs a chance that gives both skies.
        private FlightFieldDecorConfiguration _halfChanceConfiguration;
        private FlightFieldConfiguration _fieldConfiguration;
        private FieldRandomFactory _randomFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _configuration = FlightFieldEditorCompositionFactory.LoadConfiguration<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default);
            _fieldConfiguration = FlightFieldEditorCompositionFactory.LoadConfiguration<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default);
            _randomFactory = new FieldRandomFactory();
            _alwaysPlanetConfiguration = Object.Instantiate(_configuration);
            SerializedObject alwaysPlanet = new(_alwaysPlanetConfiguration);
            alwaysPlanet.FindProperty(PLANET_CHANCE_FIELD).floatValue = 1f;
            alwaysPlanet.ApplyModifiedPropertiesWithoutUndo();
            _halfChanceConfiguration = Object.Instantiate(_configuration);
            SerializedObject halfChance = new(_halfChanceConfiguration);
            halfChance.FindProperty(PLANET_CHANCE_FIELD).floatValue = HALF_PLANET_CHANCE;
            halfChance.ApplyModifiedPropertiesWithoutUndo();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            Object.DestroyImmediate(_alwaysPlanetConfiguration);
            Object.DestroyImmediate(_halfChanceConfiguration);
        }

        [Test]
        public void WhenPlanetsArePickedOnManySeeds_ThenTheSideSharesFollowTheWeights() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _fieldConfiguration, _randomFactory);
            Dictionary<FlightFieldDecorConfiguration.PlanetSide, int> countsBySide = new();
            List<FieldDecorPlanet> planets = new();
            int planetCount = 0;
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, _lightTravels[seed % _lightTravels.Length], planets);
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
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _fieldConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                foreach (Vector3 lightTravel in _lightTravels) {
                    picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, lightTravel, planets);
                    AssertClearOfCourse(planets, seed);
                }

                picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, planets);
                AssertClearOfCourse(planets, seed);
            }
        }

        [Test]
        public void WhenPlanetsArePickedWithAnyLight_ThenEveryPlanetKeepsItsOwnSizeAndStaysInsideTheFarClipPlane() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _fieldConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                foreach (Vector3 lightTravel in _lightTravels) {
                    picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, lightTravel, planets);
                    AssertSizedWithinReach(planets, seed);
                }

                picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, planets);
                AssertSizedWithinReach(planets, seed);
            }
        }

        [Test]
        public void WhenPlanetsArePickedForFieldsOfAnyWidth_ThenNoPlanetEntersTheFieldTheLayersOrTheStationZones() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _fieldConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            int tries = 0;
            int shown = 0;
            foreach (float halfWidth in _fieldHalfWidths) {
                Vector3 courseStart = new(halfWidth, 0f, 0f);
                for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                    foreach (Vector3 lightTravel in _lightTravels) {
                        picker.Fill(seed, courseStart, halfWidth, lightTravel, planets);
                        AssertClearOfFieldAndStations(planets, courseStart, halfWidth, seed);
                        tries++;
                        shown += planets.Count;
                    }

                    picker.Fill(seed, courseStart, halfWidth, planets);
                    AssertClearOfFieldAndStations(planets, courseStart, halfWidth, seed);
                    tries++;
                    shown += planets.Count;
                }
            }

            Assert.GreaterOrEqual((float)shown / tries, MIN_SHOWN_PLANET_SHARE, $"Only {shown} of {tries} flights with a planet kept it: the clearances leave too little room.");
        }

        [Test]
        public void WhenPlanetsArePickedAlongTheLight_ThenEveryPlanetStaysOnItsSideAndShowsALitCrescentAtLeast() {
            FieldDecorPlanetPicker picker = new(_alwaysPlanetConfiguration, _fieldConfiguration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            for (int seed = FIRST_SEED; seed <= SIDE_SHARE_SEEDS; seed++) {
                foreach (Vector3 lightTravel in _lightTravels) {
                    picker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, lightTravel, planets);
                    foreach (FieldDecorPlanet planet in planets)
                        AssertPlanetOnItsLitSide(planet, lightTravel, seed);
                }
            }
        }

        [Test]
        public void WhenTheSameSeedPicksPlanetsTwice_ThenThePlanetsAreTheSame() {
            FieldDecorPlanetPicker firstPicker = new(_halfChanceConfiguration, _fieldConfiguration, _randomFactory);
            FieldDecorPlanetPicker secondPicker = new(_halfChanceConfiguration, _fieldConfiguration, _randomFactory);
            List<FieldDecorPlanet> first = new();
            List<FieldDecorPlanet> second = new();
            int seedsWithPlanet = 0;
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                Vector3 lightTravel = _lightTravels[seed % _lightTravels.Length];
                firstPicker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, lightTravel, first);
                secondPicker.Fill(seed + 1, _courseStart, FIELD_HALF_WIDTH, lightTravel, second);
                secondPicker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, lightTravel, second);
                AssertSamePlanets(first, second, seed);
                firstPicker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, first);
                secondPicker.Fill(seed + 1, _courseStart, FIELD_HALF_WIDTH, second);
                secondPicker.Fill(seed, _courseStart, FIELD_HALF_WIDTH, second);
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

        // The field with its decor layers is a box across the width, along the length and up to the layers' top above
        // and below the plane; the station pads stand on the route axis inside it, anywhere along its length.
        private void AssertClearOfFieldAndStations(List<FieldDecorPlanet> planets, Vector3 courseStart, float halfWidth, int seed) {
            float layerTop = VolumeTop();
            Vector3 boxMin = new(courseStart.x - halfWidth, -layerTop, 0f);
            Vector3 boxMax = new(courseStart.x + halfWidth, layerTop, FIELD_LENGTH);
            foreach (FieldDecorPlanet planet in planets) {
                float radius = planet.Diameter / 2f;
                Vector3 nearestInBox = Vector3.Max(boxMin, Vector3.Min(boxMax, planet.Position));
                Assert.GreaterOrEqual(Vector3.Distance(planet.Position, nearestInBox) - radius, _configuration.PlanetFieldClearance - DISTANCE_TOLERANCE,
                    $"Seed {seed}, half width {halfWidth}: the {planet.Side.Name} planet {planet.Prefab.name} comes into the field or its decor layers.");
                Vector3 nearestOnAxis = new(courseStart.x, 0f, Mathf.Clamp(planet.Position.z, 0f, FIELD_LENGTH));
                Assert.GreaterOrEqual(Vector3.Distance(planet.Position, nearestOnAxis) - radius, _configuration.PlanetStationClearance - DISTANCE_TOLERANCE,
                    $"Seed {seed}, half width {halfWidth}: the {planet.Side.Name} planet {planet.Prefab.name} comes too close to a station.");
            }
        }

        // Seen from the course start the planet's far edge stays inside the far clip plane, and its size comes from its
        // own definition.
        private void AssertSizedWithinReach(List<FieldDecorPlanet> planets, int seed) {
            float viewReach = _configuration.ViewFarClipPlane - _configuration.PlanetFarClipMargin;
            foreach (FieldDecorPlanet planet in planets) {
                Vector2 diameter = FindDefinition(planet.Prefab).Diameter;
                Assert.That(planet.Diameter, Is.InRange(diameter.x - TOLERANCE, diameter.y + TOLERANCE), $"Seed {seed}: {planet.Prefab.name} is outside its own size range.");
                Assert.LessOrEqual((planet.Position - _courseStart).magnitude + planet.Diameter / 2f, viewReach + DISTANCE_TOLERANCE,
                    $"Seed {seed}: the far edge of {planet.Prefab.name} lies beyond the far clip plane minus its margin.");
            }
        }

        private FlightFieldDecorConfiguration.PlanetDefinition FindDefinition(GameObject prefab) {
            foreach (FlightFieldDecorConfiguration.PlanetDefinition definition in _configuration.Planets) {
                if (definition.Prefab == prefab)
                    return definition;
            }

            throw new KeyNotFoundException($"No planet definition has the prefab {prefab.name}.");
        }

        private void AssertPlanetOnItsLitSide(FieldDecorPlanet planet, Vector3 lightTravel, int seed) {
            Vector2 elevation = planet.Side.ElevationDegrees;
            if (elevation.x > 0f && !planet.Side.IsMirroredBelowPlane)
                Assert.Greater(planet.Position.y, 0f, $"Seed {seed}: the {planet.Side.Name} planet stands below the flight plane.");

            if (elevation.y < 0f)
                Assert.Less(planet.Position.y, 0f, $"Seed {seed}: the {planet.Side.Name} planet stands above the flight plane.");

            Assert.Less(Vector3.Angle(planet.Position - _courseStart, lightTravel), LIT_SIDE_MAX_ANGLE_DEGREES,
                $"Seed {seed}, light {lightTravel}: the {planet.Side.Name} planet shows little more than its night side.");
        }

        private float VolumeTop() {
            float top = 0f;
            foreach (FieldVolumeConfiguration.Band band in _fieldConfiguration.Volume.Bands)
                top = Mathf.Max(top, band.HeightMeters.y);

            return top;
        }
    }
}
