using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // One small rock of a debris cell: its middle in field space relative to the middle of the cell (x across the
    // field, z along it, y up from the flight plane) and the diameter its mesh stays within.
    public readonly struct FieldDebrisSpot {
        public Vector3 Offset { get; }
        public float Diameter { get; }
        public float Radius => Diameter * 0.5f;

        public FieldDebrisSpot(Vector3 offset, float diameter) {
            Offset = offset;
            Diameter = diameter;
        }
    }
}
