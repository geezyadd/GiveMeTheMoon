using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // Server: the points of interest of the generated field, where their middles stand in the world now (on the flight
    // plane). A new field changes the number; no field has none.
    public interface IFieldPointOfInterestSource {
        public bool HasField { get; }
        public int FieldNumber { get; }
        // Fills the list (cleared first) with the points in the field's order.
        public void CollectPoints(List<Vector3> worldPositions);
    }
}
