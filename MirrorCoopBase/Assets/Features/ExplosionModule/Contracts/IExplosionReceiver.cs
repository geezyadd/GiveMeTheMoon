using System.Collections.Generic;
using UnityEngine;

namespace Features.ExplosionModule.Contracts {
    // Something that reacts to an explosion itself (a ragdoll creature, a seated player) instead of being thrown as a
    // plain rigidbody. The wave reaches it once, when its front passes the receiver's centre; its parts are never
    // thrown as loose rigidbodies. Called on every peer the wave runs on: the receiver decides what applies where.
    public interface IExplosionReceiver {
        public Vector3 Center { get; }
        public IReadOnlyList<Rigidbody> Parts { get; }
        public void ReceiveExplosion(ExplosionHit hit);
    }
}
