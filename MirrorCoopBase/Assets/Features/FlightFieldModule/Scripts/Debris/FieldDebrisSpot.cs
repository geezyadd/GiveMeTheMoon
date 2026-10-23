using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // One rock of a debris cell: its middle in field space relative to the middle of the cell (x across the field, z
    // along it, y up from the flight plane), the diameter its mesh stays within and the seed of its look.
    public readonly struct FieldDebrisSpot {
        public Vector3 Offset { get; }
        public float Diameter { get; }
        public int LookSeed { get; }
        public float Radius => Diameter * 0.5f;

        public FieldDebrisSpot(Vector3 offset, float diameter, int lookSeed) {
            Offset = offset;
            Diameter = diameter;
            LookSeed = lookSeed;
        }
    }
}
