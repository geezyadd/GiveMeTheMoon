using System.Collections.Generic;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The planet of the decor comes only from the field seed (every peer and a rejoining player see the same picture)
    // and stands along the light. The volume rocks of the decor chunks: FieldVolumeLayoutTests.
    public sealed class FieldDecorLayoutTests {
        private const int SEEDS = 200;
        private const int FIRST_SEED = 1;
        private const float TOLERANCE = 0.001f;
        // The light in the test points 17 degrees below the plane and planets stand 15-55 degrees off it, so the
        // elevation adds up to this much to the azimuth spread.
        private const float PLANET_ANGLE_TOLERANCE_DEGREES = 15f;

        private FlightFieldDecorConfiguration _configuration;
        private FieldRandomFactory _randomFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _configuration = FlightFieldEditorCompositionFactory.LoadConfiguration<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default);
            _randomFactory = new FieldRandomFactory();
        }

        [Test]
        public void WhenPlanetsArePickedAlongTheLight_ThenEveryPlanetIsWithinTheAngleFromTheLight() {
            FieldDecorPlanetPicker picker = new(_configuration, _randomFactory);
            List<FieldDecorPlanet> planets = new();
            Vector3 lightTravel = new Vector3(0.6f, -0.3f, 0.7f).normalized;
            for (int seed = FIRST_SEED; seed <= SEEDS; seed++) {
                picker.Fill(seed, lightTravel, planets);
                foreach (FieldDecorPlanet planet in planets)
                    AssertPlanetAlongLight(planet, lightTravel, seed);
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
                firstPicker.Fill(seed, first);
                secondPicker.Fill(seed + 1, second);
                secondPicker.Fill(seed, second);
                Assert.AreEqual(first.Count, second.Count, $"Seed {seed}: planet count differs.");
                Assert.LessOrEqual(first.Count, _configuration.MaxPlanetsPerFlight, $"Seed {seed}: too many planets.");
                for (int i = 0; i < first.Count; i++) {
                    Assert.AreSame(first[i].Prefab, second[i].Prefab, $"Seed {seed}: planet prefab differs.");
                    Assert.Less(Vector3.Distance(first[i].Position, second[i].Position), TOLERANCE, $"Seed {seed}: planet position differs.");
                    Assert.AreEqual(first[i].Diameter, second[i].Diameter, TOLERANCE, $"Seed {seed}: planet size differs.");
                }

                seedsWithPlanet += first.Count > 0 ? 1 : 0;
            }

            Assert.That(seedsWithPlanet, Is.InRange(1, SEEDS - 1), "The planet chance gives every flight the same sky.");
        }

        private void AssertPlanetAlongLight(FieldDecorPlanet planet, Vector3 lightTravel, int seed) {
            Vector3 alongRoute = new(0f, 0f, planet.Position.z);
            Assert.Greater(Mathf.Abs(planet.Position.y), TOLERANCE, $"Seed {seed}: the planet stands in the flight plane.");
            Assert.LessOrEqual((planet.Position - alongRoute).magnitude, _configuration.PlanetDistance.y + TOLERANCE, $"Seed {seed}: the planet is too far.");
            Assert.LessOrEqual(Vector3.Angle(planet.Position - alongRoute, lightTravel), _configuration.PlanetMaxAngleFromLight + PLANET_ANGLE_TOLERANCE_DEGREES,
                $"Seed {seed}: the planet is too far from the light's direction, a player would see its night side.");
        }
    }
}
