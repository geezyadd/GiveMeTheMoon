using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;

namespace Features.FlightFieldModule.Scripts.Requests {
    // Picks the difficulty profile of a field from the profiles of its loop entry, by weight, with a random sequence
    // of its own seeded by the field seed (the generation's sequence stays the same whatever the pick).
    public sealed class FieldProfileSelector : IFieldProfileSelector
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IFieldProfileDebug
#endif
    {
        private const int PROFILE_SEED_SALT = 0x7F4A7C15;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldRandomFactory _fieldRandomFactory;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private FieldDifficultyProfile _debugProfile;
#endif

        public FieldProfileSelector(FlightFieldConfiguration flightFieldConfiguration, IFieldRandomFactory fieldRandomFactory) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldRandomFactory = fieldRandomFactory;
        }

        public FieldDifficultyProfile Select(FlightFieldConfiguration.LoopRoutes loopRoutes, int seed) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugProfile != null)
                return _debugProfile;
#endif
            return PickByWeight(loopRoutes, _fieldRandomFactory.Create(unchecked(seed ^ PROFILE_SEED_SALT)));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugUseProfile(string profileId) {
            foreach (FieldDifficultyProfile profile in _flightFieldConfiguration.DifficultyProfiles) {
                if (profile.Id != profileId)
                    continue;

                _debugProfile = profile;
                return;
            }

            throw new KeyNotFoundException($"{_flightFieldConfiguration.name}: no difficulty profile '{profileId}'.");
        }

        public void DebugClearProfile() =>
            _debugProfile = null;
#endif

        private static FieldDifficultyProfile PickByWeight(FlightFieldConfiguration.LoopRoutes loopRoutes, IFieldRandom random) =>
            FieldWeightedPick.Pick(loopRoutes.Profiles, random).Profile;
    }
}
