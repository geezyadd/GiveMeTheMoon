using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // One debris rock as the slalom check sees it: its middle on the field plane (x across, y along), its radius and
    // whether it crosses the flight plane (only those stand in the ship's way).
    public readonly struct FieldSlalomRock {
        public Vector2 Position { get; }
        public float Radius { get; }
        public bool IsInPlane { get; }

        public FieldSlalomRock(Vector2 position, float radius, bool isInPlane) {
            Position = position;
            Radius = radius;
            IsInPlane = isInPlane;
        }
    }
}
