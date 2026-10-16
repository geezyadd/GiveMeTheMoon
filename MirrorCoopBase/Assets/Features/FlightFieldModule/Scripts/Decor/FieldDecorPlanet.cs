using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // One distant planet of a flight: which prefab and where it stands in field space.
    public readonly struct FieldDecorPlanet {
        public GameObject Prefab { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Diameter { get; }

        public FieldDecorPlanet(GameObject prefab, Vector3 position, Quaternion rotation, float diameter) {
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Diameter = diameter;
        }
    }
}
