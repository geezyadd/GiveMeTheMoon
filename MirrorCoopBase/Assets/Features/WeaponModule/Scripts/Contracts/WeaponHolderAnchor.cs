using UnityEngine;

namespace Features.WeaponModule.Contracts {
    // Where a player's weapon is right now on this peer: which weapon it is and the point its shots (and its rope)
    // leave from.
    public readonly struct WeaponHolderAnchor {
        public string WeaponId { get; }
        public Vector3 Origin { get; }

        public WeaponHolderAnchor(string weaponId, Vector3 origin) {
            WeaponId = weaponId;
            Origin = origin;
        }
    }
}
