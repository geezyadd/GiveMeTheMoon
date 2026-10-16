using System;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the decor configuration: ordered ranges, a rock recipe, materials, planet sides and prefabs.
    public sealed class FlightFieldDecorConfigurationValidator : IInitializable {
        private const float MAX_ELEVATION_DEGREES = 90f;

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;

        public FlightFieldDecorConfigurationValidator(FlightFieldDecorConfiguration flightFieldDecorConfiguration) =>
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;

        public void Initialize() {
            ValidateRange(_flightFieldDecorConfiguration.LayerHeight, nameof(FlightFieldDecorConfiguration.LayerHeight));
            ValidateRange(_flightFieldDecorConfiguration.RockSize, nameof(FlightFieldDecorConfiguration.RockSize));
            ValidateRange(_flightFieldDecorConfiguration.PlanetDistance, nameof(FlightFieldDecorConfiguration.PlanetDistance));
            ValidateRange(_flightFieldDecorConfiguration.PlanetAlongRoute, nameof(FlightFieldDecorConfiguration.PlanetAlongRoute));
            ValidateRocks();
            ValidatePlanets();
        }

        private void ValidateRange(Vector2 range, string rangeName) {
            if (range.x < 0f || range.y < range.x)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: {rangeName} {range} is not a range of non-negative min <= max.");
        }

        private void ValidateRocks() {
            if (_flightFieldDecorConfiguration.RockRecipe == null || _flightFieldDecorConfiguration.RockRecipe.Rocks.Count == 0)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: no rock recipe with rocks.");

            if (_flightFieldDecorConfiguration.MaterialsByDistance.Count == 0)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: no rock materials.");

            foreach (Material material in _flightFieldDecorConfiguration.MaterialsByDistance) {
                if (material == null)
                    throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: a rock material is empty.");
            }
        }

        private void ValidatePlanets() {
            float totalWeight = 0f;
            foreach (FlightFieldDecorConfiguration.PlanetDefinition planet in _flightFieldDecorConfiguration.Planets) {
                if (planet.Prefab == null)
                    throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: a planet prefab is empty.");

                ValidateRange(planet.Diameter, $"{planet.Prefab.name} {nameof(FlightFieldDecorConfiguration.PlanetDefinition.Diameter)}");
                totalWeight += planet.Weight;
            }

            if (_flightFieldDecorConfiguration.MaxPlanetsPerFlight > 0 && totalWeight <= 0f)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: planets are allowed but no planet has a weight.");

            ValidateViewReach();
            ValidatePlanetSides();
        }

        // The picker shrinks a planet to fit between the nearest planet distance and the view reach; that room must exist.
        private void ValidateViewReach() {
            float viewReach = _flightFieldDecorConfiguration.ViewFarClipPlane - _flightFieldDecorConfiguration.PlanetFarClipMargin;
            if (viewReach <= _flightFieldDecorConfiguration.PlanetDistance.x)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: the far clip plane minus its margin ({viewReach} m) leaves no room beyond the nearest planet distance {_flightFieldDecorConfiguration.PlanetDistance.x} m.");
        }

        private void ValidatePlanetSides() {
            float totalWeight = 0f;
            foreach (FlightFieldDecorConfiguration.PlanetSide side in _flightFieldDecorConfiguration.PlanetSides) {
                ValidatePlanetSide(side);
                totalWeight += side.Weight;
            }

            if (_flightFieldDecorConfiguration.MaxPlanetsPerFlight > 0 && totalWeight <= 0f)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: planets are allowed but no planet side has a weight.");
        }

        private void ValidatePlanetSide(FlightFieldDecorConfiguration.PlanetSide side) {
            Vector2 elevation = side.ElevationDegrees;
            bool isOrderedWithinSky = elevation.x >= -MAX_ELEVATION_DEGREES && elevation.x <= elevation.y && elevation.y <= MAX_ELEVATION_DEGREES;
            if (!isOrderedWithinSky)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: planet side {side.Name} elevation {elevation} is not a range of min <= max within +-90 degrees.");

            if (side.IsMirroredBelowPlane && elevation.x < 0f)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: planet side {side.Name} is mirrored below the plane, so its elevation {elevation} must not be negative.");
        }
    }
}
