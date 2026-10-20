using System;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the volume of rocks: a volume configuration with rocks, and the same for every profile's
    // own volume, which also stays below the configuration's top (the planets keep their clearance above that); every
    // band above the plane clearance, high enough for its largest rock, with presences in 0..1 and materials.
    public sealed class FieldVolumeConfigurationValidator : IInitializable {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldVolumeConfigurationValidator(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public void Initialize() {
            FieldVolumeConfiguration volume = _flightFieldConfiguration.Volume;
            if (volume == null)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no volume configuration.");

            ValidateVolume(volume);
            float top = FieldVolumeHeights.FindTop(volume);
            foreach (FieldDifficultyProfile profile in _flightFieldConfiguration.DifficultyProfiles) {
                if (profile == null || !profile.HasOwnVolume)
                    continue;

                ValidateVolume(profile.Volume);
                if (FieldVolumeHeights.FindTop(profile.Volume) > top)
                    throw new InvalidOperationException($"{profile.Volume.name}: the volume of {profile.name} reaches above {top} m, the top of {volume.name} the planets keep clear of.");
            }
        }

        private static void ValidateVolume(FieldVolumeConfiguration volume) {
            if (volume.Rocks.Count == 0)
                throw new InvalidOperationException($"{volume.name}: no volume rock.");

            foreach (FieldClusterRockOption rock in volume.Rocks) {
                if (rock.Prefab == null)
                    throw new InvalidOperationException($"{volume.name}: a volume rock slot is empty.");
            }

            for (int i = 0; i < volume.Bands.Count; i++)
                ValidateBand(volume, volume.Bands[i], i);
        }

        private static void ValidateBand(FieldVolumeConfiguration volume, FieldVolumeConfiguration.Band band, int index) {
            float lowest = Mathf.Max(band.HeightMeters.x, volume.PlaneClearanceMeters);
            bool isRoomy = band.HeightMeters.y - lowest >= band.DiameterMeters.y + band.GapMeters && band.DiameterMeters.x > 0f && band.DiameterMeters.x <= band.DiameterMeters.y;
            if (!isRoomy)
                throw new InvalidOperationException($"{volume.name}: band {index} is not high enough above the plane clearance for its largest rock, or its diameters are not a range.");

            bool isPresenceValid = band.Presence.x >= 0f && band.Presence.y <= 1f && band.Presence.x <= band.Presence.y;
            if (!isPresenceValid)
                throw new InvalidOperationException($"{volume.name}: band {index} has presences outside 0..1.");

            if (band.Materials.Count == 0)
                throw new InvalidOperationException($"{volume.name}: band {index} has no material.");

            foreach (Material material in band.Materials) {
                if (material == null)
                    throw new InvalidOperationException($"{volume.name}: band {index} has an empty material slot.");
            }
        }
    }
}
