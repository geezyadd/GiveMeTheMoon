using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // Every peer, the server included: the ore of the current field from the synced live field, where each piece is
    // now on this peer. The server's checks (delivery to the ship) and what a peer shows read the same pieces. The
    // results list is the caller's: it is cleared and filled, so a caller keeps one list and allocates nothing.
    public interface IFieldOreQuery {
        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldOreDescription> results);
        public void FindAll(List<FieldOreDescription> results);
        public bool TryGetOre(FieldOreId id, out FieldOreDescription ore);
    }
}
