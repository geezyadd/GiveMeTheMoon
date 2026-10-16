using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // One grid point of the debris layout: where its rock would stand on the field plane (x across, y along) and at
    // what height, how big it would be, whether the point holds a rock and its priority against close neighbours.
    public readonly struct FieldDebrisCandidate {
        public Vector2 Position { get; }
        public float Height { get; }
        public float Diameter { get; }
        public float Priority { get; }
        public bool Exists { get; }
        public int LookSeed { get; }
        public float Radius => Diameter * 0.5f;

        public FieldDebrisCandidate(Vector2 position, float height, float diameter, float priority, bool exists, int lookSeed) {
            Position = position;
            Height = height;
            Diameter = diameter;
            Priority = priority;
            Exists = exists;
            LookSeed = lookSeed;
        }
    }
}
