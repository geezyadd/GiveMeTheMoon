using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // A giant of an obstacle field as the volume sees it: the sphere its rock stays within, its middle on the flight
    // plane (field space: x across, y along). No volume rock comes into it.
    public readonly struct FieldVolumeGiant {
        public Vector2 Center { get; }
        public float Radius { get; }

        public FieldVolumeGiant(Vector2 center, float radius) {
            Center = center;
            Radius = radius;
        }
    }
}
