using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // One grid point of a volume band: where its rock would stand (field space), how big it would be, whether the
    // point holds a rock at all and its priority against neighbours that come too close.
    public readonly struct FieldVolumeCandidate {
        public Vector3 Position { get; }
        public float Radius { get; }
        public float Priority { get; }
        public bool Exists { get; }
        public int LookSeed { get; }

        public FieldVolumeCandidate(Vector3 position, float radius, float priority, bool exists, int lookSeed) {
            Position = position;
            Radius = radius;
            Priority = priority;
            Exists = exists;
            LookSeed = lookSeed;
        }
    }
}
