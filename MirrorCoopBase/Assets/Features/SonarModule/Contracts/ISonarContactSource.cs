using System.Collections.Generic;
using UnityEngine;

namespace Features.SonarModule.Contracts {
    // One family of sonar contacts (rocks, the station, targets, enemies), read from data this peer already has.
    // A source owns its contact ids: TryGetContact answers only for ids it gave out.
    public interface ISonarContactSource {
        // Adds the contacts within the radius of the center; the list is the caller's and is not cleared.
        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results);
        public bool TryGetContact(string contactId, out SonarContact contact);
    }
}
