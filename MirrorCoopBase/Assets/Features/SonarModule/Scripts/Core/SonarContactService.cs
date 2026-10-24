using System.Collections.Generic;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // Every bound ISonarContactSource as one: a new family of contacts is a new source binding, the sonar does not
    // change.
    public sealed class SonarContactService : ISonarContactService {
        private readonly List<ISonarContactSource> _sonarContactSources;
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarContactService(List<ISonarContactSource> sonarContactSources, SonarConfiguration sonarConfiguration) {
            _sonarContactSources = sonarContactSources;
            _sonarConfiguration = sonarConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            results.Clear();
            for (int i = 0; i < _sonarContactSources.Count; i++)
                _sonarContactSources[i].CollectContacts(center, radius, results);
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            for (int i = 0; i < _sonarContactSources.Count; i++) {
                if (_sonarContactSources[i].TryGetContact(contactId, out contact))
                    return true;
            }

            return false;
        }

        public bool TryGetKind(string kindId, out SonarContactKind kind) {
            kind = null;
            IReadOnlyList<SonarContactKind> kinds = _sonarConfiguration.Kinds;
            for (int i = 0; i < kinds.Count; i++) {
                if (kinds[i].Id != kindId)
                    continue;

                kind = kinds[i];
                return true;
            }

            return false;
        }
    }
}
