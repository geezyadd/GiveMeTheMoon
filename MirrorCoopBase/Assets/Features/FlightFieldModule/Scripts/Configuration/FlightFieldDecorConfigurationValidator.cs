using System;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the decor configuration: ordered ranges, a rock recipe, materials and planet prefabs.
    public sealed class FlightFieldDecorConfigurationValidator : IInitializable {
        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;

        public FlightFieldDecorConfigurationValidator(FlightFieldDecorConfiguration flightFieldDecorConfiguration) =>
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;

        public void Initialize() {
            ValidateRange(_flightFieldDecorConfiguration.LayerHeight, nameof(FlightFieldDecorConfiguration.LayerHeight));
            ValidateRange(_flightFieldDecorConfiguration.RockSize, nameof(FlightFieldDecorConfiguration.RockSize));
            ValidateRange(_flightFieldDecorConfiguration.PlanetDistance, nameof(FlightFieldDecorConfiguration.PlanetDistance));
            ValidateRange(_flightFieldDecorConfiguration.PlanetElevationDegrees, nameof(FlightFieldDecorConfiguration.PlanetElevationDegrees));
            ValidateRange(_flightFieldDecorConfiguration.PlanetAlongRoute, nameof(FlightFieldDecorConfiguration.PlanetAlongRoute));
            ValidateRange(_flightFieldDecorConfiguration.PlanetDiameter, nameof(FlightFieldDecorConfiguration.PlanetDiameter));
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
            if (_flightFieldDecorConfiguration.MaxPlanetsPerFlight > 0 && _flightFieldDecorConfiguration.PlanetPrefabs.Count == 0)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: planets are allowed but there is no planet prefab.");

            foreach (GameObject planet in _flightFieldDecorConfiguration.PlanetPrefabs) {
                if (planet == null)
                    throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: a planet prefab is empty.");
            }
        }
    }
}
