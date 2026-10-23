using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // The world query of ray-based deliveries; the physics implementation decides the layers a weapon can hit.
    public interface IWeaponRaycast {
        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit);
    }
}
