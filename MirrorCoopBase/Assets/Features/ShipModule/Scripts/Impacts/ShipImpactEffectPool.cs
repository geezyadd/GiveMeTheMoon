using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // Impact effects made once and played again: an effect disables itself when it is over and is then free. A few
    // play at once at most, so finding a free one walks the list. Effects stand in the game scene and go with it.
    public sealed class ShipImpactEffectPool : IShipImpactEffectPool {
        private static readonly Predicate<ShipImpactEffect> _isDestroyed = IsDestroyed;

        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;
        private readonly List<ShipImpactEffect> _effects = new();

        public ShipImpactEffectPool(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public void Play(Vector3 position, float size) =>
            TakeFree().Play(position, size);

        private ShipImpactEffect TakeFree() {
            _effects.RemoveAll(_isDestroyed);
            foreach (ShipImpactEffect effect in _effects) {
                if (!effect.IsPlaying)
                    return effect;
            }

            ShipImpactEffect created = UnityEngine.Object.Instantiate(_shipRockImpactConfiguration.ImpactEffect);
            _effects.Add(created);
            return created;
        }

        private static bool IsDestroyed(ShipImpactEffect effect) =>
            effect == null;
    }
}
