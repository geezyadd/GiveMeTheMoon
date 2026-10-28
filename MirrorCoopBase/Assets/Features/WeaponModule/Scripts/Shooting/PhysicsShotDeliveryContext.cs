using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    public sealed class PhysicsShotDeliveryContext : IShotDeliveryContext {
        public IWeaponRandom Random { get; } = new UnityWeaponRandom();

        public IWeaponRaycast CreateRaycast(LayerMask hitLayers) =>
            new PhysicsWeaponRaycast(hitLayers);

        public IWeaponRaycast CreateSphereCast(LayerMask hitLayers, float radiusMeters) =>
            new PhysicsWeaponRaycast(hitLayers, radiusMeters);
    }
}
