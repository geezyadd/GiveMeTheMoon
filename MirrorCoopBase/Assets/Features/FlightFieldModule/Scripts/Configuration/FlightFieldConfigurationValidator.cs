using System;
using System.Collections.Generic;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the flight field configuration: routes for loop 0 on, every loop entry with a profile of
    // the configuration's list; release radii not below their build radii.
    public sealed class FlightFieldConfigurationValidator : IInitializable {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FlightFieldConfigurationValidator(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public void Initialize() {
            ValidateRoutes();
            ValidateStreaming();
        }

        private void ValidateStreaming() {
            if (_flightFieldConfiguration.ReleaseRadius < _flightFieldConfiguration.BuildRadius)
                throw new InvalidOperationException(
                    $"{_flightFieldConfiguration.name}: the release radius ({_flightFieldConfiguration.ReleaseRadius} m) is below the build radius ({_flightFieldConfiguration.BuildRadius} m).");

            if (_flightFieldConfiguration.DebrisReleaseRadius < _flightFieldConfiguration.DebrisBuildRadius)
                throw new InvalidOperationException(
                    $"{_flightFieldConfiguration.name}: the debris release radius ({_flightFieldConfiguration.DebrisReleaseRadius} m) is below the debris build radius ({_flightFieldConfiguration.DebrisBuildRadius} m).");
        }

        private void ValidateRoutes() {
            bool hasFirstLoop = false;
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _flightFieldConfiguration.RoutesByLoop) {
                if (loopRoutes.RouteDifficulties.Count == 0)
                    throw new InvalidOperationException($"{_flightFieldConfiguration.name}: routes from loop {loopRoutes.FromLoop} have no route.");

                ValidateProfiles(loopRoutes);
                hasFirstLoop |= loopRoutes.FromLoop == 0;
            }

            if (!hasFirstLoop)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no routes from loop 0.");
        }

        private void ValidateProfiles(FlightFieldConfiguration.LoopRoutes loopRoutes) {
            float totalWeight = 0f;
            foreach (FlightFieldConfiguration.ProfileOption option in loopRoutes.Profiles) {
                if (option.Profile == null || !Contains(_flightFieldConfiguration.DifficultyProfiles, option.Profile))
                    throw new InvalidOperationException($"{_flightFieldConfiguration.name}: a profile of loops from {loopRoutes.FromLoop} is empty or missing from the profile list.");

                totalWeight += option.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: loops from {loopRoutes.FromLoop} have no profile with a positive weight.");
        }

        private static bool Contains(IReadOnlyList<FieldDifficultyProfile> profiles, FieldDifficultyProfile profile) {
            foreach (FieldDifficultyProfile candidate in profiles) {
                if (candidate == profile)
                    return true;
            }

            return false;
        }
    }
}
