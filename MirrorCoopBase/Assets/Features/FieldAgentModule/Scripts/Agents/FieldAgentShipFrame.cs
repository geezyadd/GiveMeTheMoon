using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Agents {
    // The ship's pose and velocity for one server tick: the frame every ship-space agent lives in.
    public readonly struct FieldAgentShipFrame {
        public bool HasShip { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; }

        public FieldAgentShipFrame(bool hasShip, Vector3 position, Quaternion rotation, Vector3 velocity) {
            HasShip = hasShip;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
        }

        public Vector3 ToWorldPoint(Vector3 shipLocal) =>
            Position + Rotation * shipLocal;

        public Vector3 ToShipPoint(Vector3 world) =>
            Quaternion.Inverse(Rotation) * (world - Position);

        public Vector3 ToWorldDirection(Vector3 shipLocal) =>
            Rotation * shipLocal;

        public Vector3 ToShipDirection(Vector3 world) =>
            Quaternion.Inverse(Rotation) * world;

        public Quaternion ToWorldRotation(Quaternion shipLocal) =>
            Rotation * shipLocal;

        public Quaternion ToShipRotation(Quaternion world) =>
            Quaternion.Inverse(Rotation) * world;
    }
}
