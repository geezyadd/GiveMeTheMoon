using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // The world query of ray-based deliveries; the physics implementation decides the layers a weapon can hit. The
    // body of the given network id (the shooter's own) is never struck; 0 ignores none.
    public interface IWeaponRaycast {
        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, uint ignoredReceiverId, out WeaponRaycastHit hit);
    }
}
