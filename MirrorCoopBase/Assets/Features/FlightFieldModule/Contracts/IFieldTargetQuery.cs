using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // Server: the target balloons of the current field from its data, built in the scene or not. Popped balloons are
    // left out. The results list is the caller's: it is cleared and filled, so a caller keeps one list and allocates
    // nothing.
    public interface IFieldTargetQuery {
        public void FindInSphere(Vector3 center, float radius, List<FieldTargetDescription> results);
        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldTargetDescription> results);
        public bool TryGetTarget(FieldTargetId id, out FieldTargetDescription target);
    }
}
