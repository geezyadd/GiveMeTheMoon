using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    public interface IFieldTargetRockClearance {
        // True when no rock of the field reaches into the sphere around a point in field space (x across, y up, z
        // along). The rocks are read from the field data, as the server and every peer lay them out.
        public bool IsClear(FieldTargetRockWorkspace workspace, FieldFrame frame, Vector3 fieldPosition, float radius);
    }
}
