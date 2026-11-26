using Mirror;
using UnityEngine;

namespace Features.ExplosionModule.Network {
    // An explosion went off on the server: its world centre and its preset's index in the configuration.
    public struct ExplosionMessage : NetworkMessage {
        public Vector3 Center;
        public byte PresetIndex;
    }
}
