using Features.FieldAgentModule.Scripts.Agents;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // One agent as a client shows it now: world pose in flight, ship-local pose in the ship modes.
    public readonly struct FieldAgentPose {
        public int Id { get; }
        public byte KindIndex { get; }
        public FieldAgentMode Mode { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool IsInShipSpace => Mode != FieldAgentMode.Flight;

        public FieldAgentPose(int id, byte kindIndex, FieldAgentMode mode, Vector3 position, Quaternion rotation) {
            Id = id;
            KindIndex = kindIndex;
            Mode = mode;
            Position = position;
            Rotation = rotation;
        }
    }
}
