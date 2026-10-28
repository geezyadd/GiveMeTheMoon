using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;

namespace Features.BugModule.Scripts.Configuration {
    // Finds the bug's kind among the field agent kinds by its id: a player build may hold a copy of the profile apart
    // from the one in the list, so a reference match is not enough.
    public sealed class BugKindService : IBugKindService {
        private readonly BugConfiguration _bugConfiguration;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public BugKindService(BugConfiguration bugConfiguration, FieldAgentConfiguration fieldAgentConfiguration) {
            _bugConfiguration = bugConfiguration;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public bool TryGetKindIndex(out byte kindIndex) {
            kindIndex = 0;
            IReadOnlyList<FieldAgentProfile> profiles = _fieldAgentConfiguration.Profiles;
            for (int i = 0; i < profiles.Count; i++) {
                if (IsBugProfile(profiles[i]) == false)
                    continue;

                kindIndex = (byte)i;
                return true;
            }

            return false;
        }

        public bool IsBug(byte kindIndex) =>
            kindIndex < _fieldAgentConfiguration.Profiles.Count && IsBugProfile(_fieldAgentConfiguration.Profiles[kindIndex]);

        private bool IsBugProfile(FieldAgentProfile profile) =>
            profile.Id == _bugConfiguration.Profile.Id;
    }
}
