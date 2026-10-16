using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Which planets a flight shows and where, from the field seed (and the main light, the same on every peer), so
    // every peer sees the same sky. Positions are in field space: x across the route, y up from the flight plane,
    // z along the route. The side of the sky (near the horizon, above, below) comes from the seed and the side
    // weights only; the light just turns the planet within that side so its lit side faces the ship.
    public sealed class FieldDecorPlanetPicker : IFieldDecorPlanetPicker {
        // Keeps the planet sequence apart from the structure and decor rock sequences of the same seed.
        private const int PLANET_SEED_SALT = 1640531527;
        private const float HALF = 0.5f;

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        private readonly IFieldRandom _random;

        public FieldDecorPlanetPicker(FlightFieldDecorConfiguration flightFieldDecorConfiguration, IFieldRandomFactory fieldRandomFactory) {
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
            _random = fieldRandomFactory.Create(0);
        }

        public void Fill(int fieldSeed, Vector3 courseStart, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance)
                    planets.Add(CreatePlanet(courseStart, CreateAcrossRoute()));
            }
        }

        public void Fill(int fieldSeed, Vector3 courseStart, Vector3 lightTravel, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance)
                    planets.Add(CreatePlanet(courseStart, lightTravel.normalized));
            }
        }

        // Every planet takes the same number of random values, so the result depends on the seed and the light only.
        private FieldDecorPlanet CreatePlanet(Vector3 courseStart, Vector3 preferredDirection) {
            FlightFieldDecorConfiguration.PlanetSide side = PickSide();
            bool isMirrorRollBelow = _random.Value() < HALF;
            float upOrDown = side.IsMirroredBelowPlane && isMirrorRollBelow ? -1f : 1f;
            Vector3 direction = CreateDirectionTowards(preferredDirection, side, upOrDown);
            IReadOnlyList<GameObject> prefabs = _flightFieldDecorConfiguration.PlanetPrefabs;
            GameObject prefab = prefabs[_random.Range(0, prefabs.Count)];
            Vector3 offset = direction * Lerp(_flightFieldDecorConfiguration.PlanetDistance);
            float alongRoute = ClampAlongRoute(Lerp(_flightFieldDecorConfiguration.PlanetAlongRoute), offset);
            float diameter = Lerp(_flightFieldDecorConfiguration.PlanetDiameter);
            Vector3 position = courseStart + Vector3.forward * alongRoute + offset;
            return new FieldDecorPlanet(prefab, position, FieldClusterLayout.RandomRotation(_random), diameter, side);
        }

        private FlightFieldDecorConfiguration.PlanetSide PickSide() {
            IReadOnlyList<FlightFieldDecorConfiguration.PlanetSide> sides = _flightFieldDecorConfiguration.PlanetSides;
            float totalWeight = 0f;
            foreach (FlightFieldDecorConfiguration.PlanetSide side in sides)
                totalWeight += side.Weight;

            float roll = _random.Value() * totalWeight;
            foreach (FlightFieldDecorConfiguration.PlanetSide side in sides) {
                roll -= side.Weight;
                if (roll < 0f)
                    return side;
            }

            return LastWeightedSide(sides);
        }

        // With no main light the planet leans to one side of the route.
        private Vector3 CreateAcrossRoute() =>
            _random.Value() < HALF ? Vector3.right : Vector3.left;

        // Within the side's elevation and the light's azimuth spread, the try closest to the preferred direction wins;
        // every try is first turned out of the course ahead.
        private Vector3 CreateDirectionTowards(Vector3 preferredDirection, FlightFieldDecorConfiguration.PlanetSide side, float upOrDown) {
            float preferredAzimuth = Mathf.Atan2(preferredDirection.x, preferredDirection.z) * Mathf.Rad2Deg;
            float maxAngle = _flightFieldDecorConfiguration.PlanetMaxAngleFromLight;
            Vector3 closest = Vector3.zero;
            float closestAngle = float.MaxValue;
            for (int i = 0; i < _flightFieldDecorConfiguration.PlanetPlacementTries; i++) {
                float elevation = upOrDown * Lerp(side.ElevationDegrees);
                float azimuth = TurnOutOfCourse(preferredAzimuth + Mathf.Lerp(-maxAngle, maxAngle, _random.Value()), elevation);
                Vector3 direction = CreateDirection(azimuth, elevation);
                float angle = Vector3.Angle(direction, preferredDirection);
                if (angle >= closestAngle)
                    continue;

                closest = direction;
                closestAngle = angle;
            }

            return closest;
        }

        // The angle from the course is acos(cos(elevation) * cos(azimuth)), so the azimuth is pushed out to the
        // smallest one that keeps the minimum angle (none is needed when the elevation alone is enough).
        private float TurnOutOfCourse(float azimuthDegrees, float elevationDegrees) {
            float minCos = Mathf.Cos(_flightFieldDecorConfiguration.PlanetMinAngleFromCourse * Mathf.Deg2Rad);
            float elevationCos = Mathf.Cos(elevationDegrees * Mathf.Deg2Rad);
            float minAzimuth = elevationCos <= minCos ? 0f : Mathf.Acos(minCos / elevationCos) * Mathf.Rad2Deg;
            float azimuth = Mathf.DeltaAngle(0f, azimuthDegrees);
            float turned = Mathf.Max(Mathf.Abs(azimuth), minAzimuth);
            return azimuth < 0f ? -turned : turned;
        }

        // A planet further along the route looks closer to the course from the start; it moves back until the
        // course start sees it at least the minimum angle off the course.
        private float ClampAlongRoute(float alongRoute, Vector3 offset) {
            float lateral = new Vector2(offset.x, offset.y).magnitude;
            float maxAlongRoute = lateral / Mathf.Tan(_flightFieldDecorConfiguration.PlanetMinAngleFromCourse * Mathf.Deg2Rad) - offset.z;
            return Mathf.Min(alongRoute, Mathf.Max(0f, maxAlongRoute));
        }

        // Azimuth 0 is along the route, 90 across it; elevation is up from the flight plane, negative below it.
        private static Vector3 CreateDirection(float azimuthDegrees, float elevationDegrees) {
            float elevation = elevationDegrees * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;
            float flat = Mathf.Cos(elevation);
            return new Vector3(flat * Mathf.Sin(azimuth), Mathf.Sin(elevation), flat * Mathf.Cos(azimuth));
        }

        // Float rounding can leave the roll at the total; the last side with a weight takes it.
        private static FlightFieldDecorConfiguration.PlanetSide LastWeightedSide(IReadOnlyList<FlightFieldDecorConfiguration.PlanetSide> sides) {
            for (int i = sides.Count - 1; i > 0; i--) {
                if (sides[i].Weight > 0f)
                    return sides[i];
            }

            return sides[0];
        }

        private float Lerp(Vector2 range) =>
            Mathf.Lerp(range.x, range.y, _random.Value());
    }
}
