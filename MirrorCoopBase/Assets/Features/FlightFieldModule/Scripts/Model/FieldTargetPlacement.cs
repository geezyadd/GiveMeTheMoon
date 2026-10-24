using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // One target balloon of a generated field: its kind (a FieldTargetCatalog id) and its middle in field space (the
    // field root's local space: x along Right, y up from the flight plane, z along Forward), so a world shift moves it
    // with the field.
    public readonly struct FieldTargetPlacement {
        public string KindId { get; }
        public Vector3 FieldPosition { get; }

        public FieldTargetPlacement(string kindId, Vector3 fieldPosition) {
            KindId = kindId;
            FieldPosition = fieldPosition;
        }
    }
}
