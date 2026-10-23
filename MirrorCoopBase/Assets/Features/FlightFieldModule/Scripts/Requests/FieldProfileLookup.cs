using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Requests {
    // Resolves the profile id a field carries over the network against the configuration's profiles.
    public sealed class FieldProfileLookup : IFieldProfileLookup {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldProfileLookup(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public FieldDifficultyProfile GetRequired(string id) {
            if (!TryFind(id, out FieldDifficultyProfile profile))
                throw new KeyNotFoundException($"{_flightFieldConfiguration.name}: no difficulty profile '{id}'.");

            return profile;
        }

        public bool TryFind(string id, out FieldDifficultyProfile profile) {
            profile = null;
            foreach (FieldDifficultyProfile candidate in _flightFieldConfiguration.DifficultyProfiles) {
                if (candidate.Id != id)
                    continue;

                profile = candidate;
                return true;
            }

            return false;
        }
    }
}
