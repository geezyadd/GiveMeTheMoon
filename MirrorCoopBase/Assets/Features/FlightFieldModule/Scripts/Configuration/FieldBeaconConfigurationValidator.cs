using System;
using Features.FlightFieldModule.Scripts.Beacons;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the beacons: a beacon configuration with a prefab whose light parts all have a renderer
    // and a material, a dim point not above the bright one and a wall search at least one step long.
    public sealed class FieldBeaconConfigurationValidator : IInitializable {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldBeaconConfigurationValidator(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public void Initialize() {
            FieldBeaconConfiguration beacons = _flightFieldConfiguration.Beacons;
            if (beacons == null)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no beacon configuration.");

            if (beacons.Prefab == null)
                throw new InvalidOperationException($"{beacons.name}: no beacon prefab.");

            ValidateLights(beacons.Prefab);

            if (beacons.BlinkIntensity.x < 0f || beacons.BlinkIntensity.x > beacons.BlinkIntensity.y)
                throw new InvalidOperationException($"{beacons.name}: the blink intensity ({beacons.BlinkIntensity}) is not a range from 0 up.");

            if (beacons.EdgeSearchCells * _flightFieldConfiguration.CellSize < beacons.EdgeSearchStepMeters)
                throw new InvalidOperationException($"{beacons.name}: the wall search ({beacons.EdgeSearchCells} cells) is shorter than one step ({beacons.EdgeSearchStepMeters} m).");
        }

        private static void ValidateLights(FieldBeaconVisual prefab) {
            if (prefab.Lights.Count == 0)
                throw new InvalidOperationException($"{prefab.name}: the beacon prefab has no light part.");

            foreach (Renderer lightRenderer in prefab.Lights) {
                if (lightRenderer == null || lightRenderer.sharedMaterial == null)
                    throw new InvalidOperationException($"{prefab.name}: a light part of the beacon prefab has no renderer or no material.");
            }
        }
    }
}
