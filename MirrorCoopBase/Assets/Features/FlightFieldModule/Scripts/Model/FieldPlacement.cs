using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // One structure standing in the field; the list of placements is the content of the field.
    public readonly struct FieldPlacement {
        // Unique inside one field; the cells under the placement store it.
        public int Id { get; }
        public string StructureId { get; }
        // The cell of the first corner (smallest x and y) of the rotated footprint box.
        public Vector2Int Anchor { get; }
        public FieldRotation Rotation { get; }

        public FieldPlacement(int id, string structureId, Vector2Int anchor, FieldRotation rotation) {
            Id = id;
            StructureId = structureId;
            Anchor = anchor;
            Rotation = rotation;
        }
    }
}
