using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Which planets a flight shows and where, from the field seed (and the main light, the same on every peer), so
    // every peer sees the same sky. Positions are in field space: x across the route, y up from the flight plane,
    // z along the route; a planet always stands above or below the plane.
    public sealed class FieldDecorPlanetPicker : IFieldDecorPlanetPicker {
        // Keeps the planet sequence apart from the structure and decor rock sequences of the same seed.
        private const int PLANET_SEED_SALT = 1640531527;
        private const float SIDE_AZIMUTH_DEGREES = 90f;
        private const float HALF = 0.5f;

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        private readonly IFieldRandom _random;

        public FieldDecorPlanetPicker(FlightFieldDecorConfiguration flightFieldDecorConfiguration, IFieldRandomFactory fieldRandomFactory) {
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
            _random = fieldRandomFactory.Create(0);
        }

        public void Fill(int fieldSeed, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance)
                    planets.Add(CreatePlanet(CreateSideDirection()));
            }
        }

        public void Fill(int fieldSeed, Vector3 lightTravel, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance)
                    planets.Add(CreatePlanet(CreateDirectionAlong(lightTravel.normalized)));
            }
        }

        // On the light's side of the plane (a light shining down puts the planet below, lit from above). Every try takes
        // the same number of random values, so the result depends on the seed and the light only.
        private Vector3 CreateDirectionAlong(Vector3 lightTravel) {
            float lightAzimuth = Mathf.Atan2(lightTravel.x, lightTravel.z) * Mathf.Rad2Deg;
            float upOrDown = lightTravel.y < 0f ? -1f : 1f;
            float maxAngle = _flightFieldDecorConfiguration.PlanetMaxAngleFromLight;
            Vector3 closest = Vector3.zero;
            float closestAngle = float.MaxValue;
            for (int i = 0; i < _flightFieldDecorConfiguration.PlanetPlacementTries; i++) {
                Vector3 direction = CreateDirection(lightAzimuth + Mathf.Lerp(-maxAngle, maxAngle, _random.Value()), upOrDown);
                float angle = Vector3.Angle(direction, lightTravel);
                if (angle >= closestAngle)
                    continue;

                closest = direction;
                closestAngle = angle;
            }

            return closest;
        }

        private Vector3 CreateSideDirection() {
            float azimuth = _random.Value() < HALF ? SIDE_AZIMUTH_DEGREES : -SIDE_AZIMUTH_DEGREES;
            return CreateDirection(azimuth, _random.Value() < HALF ? -1f : 1f);
        }

        // Azimuth 0 is along the route, 90 across it; the elevation keeps the direction above or below the plane.
        private Vector3 CreateDirection(float azimuthDegrees, float upOrDown) {
            float elevation = Lerp(_flightFieldDecorConfiguration.PlanetElevationDegrees) * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;
            float flat = Mathf.Cos(elevation);
            return new Vector3(flat * Mathf.Sin(azimuth), upOrDown * Mathf.Sin(elevation), flat * Mathf.Cos(azimuth));
        }

        private FieldDecorPlanet CreatePlanet(Vector3 direction) {
            IReadOnlyList<GameObject> prefabs = _flightFieldDecorConfiguration.PlanetPrefabs;
            GameObject prefab = prefabs[_random.Range(0, prefabs.Count)];
            float distance = Lerp(_flightFieldDecorConfiguration.PlanetDistance);
            Vector3 alongRoute = new(0f, 0f, Lerp(_flightFieldDecorConfiguration.PlanetAlongRoute));
            float diameter = Lerp(_flightFieldDecorConfiguration.PlanetDiameter);
            return new FieldDecorPlanet(prefab, alongRoute + direction * distance, FieldClusterLayout.RandomRotation(_random), diameter);
        }

        private float Lerp(Vector2 range) =>
            Mathf.Lerp(range.x, range.y, _random.Value());
    }
}
