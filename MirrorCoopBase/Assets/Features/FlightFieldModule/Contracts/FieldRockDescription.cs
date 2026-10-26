using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // A rock as the field data describes it, wherever it is built: its id, the structure or debris kind it belongs
    // to, its middle in the world now and the diameter its mesh stays within.
    public readonly struct FieldRockDescription {
        public FieldRockId Id { get; }
        // The structure id of a cluster rock, the debris id of a debris rock.
        public string SourceId { get; }
        public Vector3 Center { get; }
        public float Diameter { get; }
        public FieldRockSize Size { get; }
        public bool IsBreakable { get; }
        public float Radius => Diameter * 0.5f;

        public FieldRockDescription(FieldRockId id, string sourceId, Vector3 center, float diameter, FieldRockSize size, bool isBreakable) {
            Id = id;
            SourceId = sourceId;
            Center = center;
            Diameter = diameter;
            Size = size;
            IsBreakable = isBreakable;
        }
    }
}
