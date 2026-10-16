using System;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the decor configuration: ordered ranges and planet prefabs.
    public sealed class FlightFieldDecorConfigurationValidator : IInitializable {
        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;

        public FlightFieldDecorConfigurationValidator(FlightFieldDecorConfiguration flightFieldDecorConfiguration) =>
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;

        public void Initialize() {
            ValidateRange(_flightFieldDecorConfiguration.PlanetDistance, nameof(FlightFieldDecorConfiguration.PlanetDistance));
            ValidateRange(_flightFieldDecorConfiguration.PlanetElevationDegrees, nameof(FlightFieldDecorConfiguration.PlanetElevationDegrees));
            ValidateRange(_flightFieldDecorConfiguration.PlanetAlongRoute, nameof(FlightFieldDecorConfiguration.PlanetAlongRoute));
            ValidateRange(_flightFieldDecorConfiguration.PlanetDiameter, nameof(FlightFieldDecorConfiguration.PlanetDiameter));
            ValidatePlanets();
        }

        private void ValidateRange(Vector2 range, string rangeName) {
            if (range.x < 0f || range.y < range.x)
                throw new InvalidOperationException($"{_flightFieldDecorConfiguration.name}: {rangeName} {range} is not a range of non-negative min <= max.");
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
