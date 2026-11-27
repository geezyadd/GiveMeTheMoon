using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Impacts {
    // The bodies that react to shots on this peer, found by a struck rigidbody or by their network id.
    public interface IWeaponImpactLookup {
        public bool TryFindByPart(Rigidbody part, out IWeaponImpactReceiver receiver, out int partIndex);

        public bool TryGet(uint networkId, out IWeaponImpactReceiver receiver);
    }
}
