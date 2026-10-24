using System.Collections.Generic;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarContactService {
        // Clears the list and fills it with the contacts of every source.
        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results);
        public bool TryGetContact(string contactId, out SonarContact contact);
        public bool TryGetKind(string kindId, out SonarContactKind kind);
    }
}
