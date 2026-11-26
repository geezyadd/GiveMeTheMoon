using System.Collections.Generic;
using UnityEngine;

namespace Features.WeaponModule.Contracts {
    // A body that reacts to the shots that strike it, on every peer (a player's ragdoll). Its parts are the rigidbodies
    // a shot can strike; a struck part goes over the network as its index, the body as its network id.
    public interface IWeaponImpactReceiver {
        public uint NetworkId { get; }
        public IReadOnlyList<Rigidbody> Parts { get; }

        public void ReceiveImpact(in WeaponImpact impact);
    }
}
