using Mirror;
using UnityEngine;

namespace Features.ExplosionModule.Network {
    // An explosion went off on the server: its world centre, its preset's index in the configuration and whether the
    // peers show its effect (a projectile's explosion is shown by each peer's own projectile where it ends).
    public struct ExplosionMessage : NetworkMessage {
        public Vector3 Center;
        public byte PresetIndex;
        public bool ShowsEffect;
    }
}
