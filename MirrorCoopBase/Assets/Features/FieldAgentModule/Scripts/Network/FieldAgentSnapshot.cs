using System;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Network {
    // One agent in a batch: 22 bytes on the wire. Position and rotation are world values in flight and ship-local in
    // the ship modes (Mode, a FieldAgentMode byte); the rotation is Mirror's smallest-three compressed quaternion.
    [Serializable]
    public struct FieldAgentSnapshot {
        public int Id;
        public byte KindIndex;
        public byte Mode;
        public Vector3 Position;
        public uint Rotation;

        public FieldAgentSnapshot(int id, byte kindIndex, byte mode, Vector3 position, uint rotation) {
            Id = id;
            KindIndex = kindIndex;
            Mode = mode;
            Position = position;
            Rotation = rotation;
        }
    }
}
