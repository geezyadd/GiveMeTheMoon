using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // Server: the rocks of the current field from its data, built in the scene or not. Broken rocks are left out.
    // The results list is the caller's: it is cleared and filled, so a caller keeps one list and allocates nothing.
    public interface IFieldRockQuery {
        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results);
        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results);
        public bool TryGetRock(FieldRockId id, out FieldRockDescription rock);
    }
}
