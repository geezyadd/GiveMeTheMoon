using Features.WeaponModule.Scripts.Impacts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    public sealed class PhysicsShotDeliveryContext : IShotDeliveryContext {
        private readonly IWeaponImpactLookup _impactLookup;

        public PhysicsShotDeliveryContext(IWeaponImpactLookup impactLookup) =>
            _impactLookup = impactLookup;

        public IWeaponRandom Random { get; } = new UnityWeaponRandom();

        public IWeaponRaycast CreateRaycast(LayerMask hitLayers) =>
            new PhysicsWeaponRaycast(hitLayers, _impactLookup);
    }
}
