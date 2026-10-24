using Features.EffectPoolModule.Scripts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // The impact effects stand in the game scene (no parent) and go with it.
    public sealed class ShipImpactEffectPool : IShipImpactEffectPool {
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;
        private readonly SelfDisablingEffectPool<ShipImpactEffect> _effects = new();

        public ShipImpactEffectPool(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public void Play(Vector3 position, float size) =>
            _effects.TakeFree(_shipRockImpactConfiguration.ImpactEffect, null).Play(position, size);
    }
}
