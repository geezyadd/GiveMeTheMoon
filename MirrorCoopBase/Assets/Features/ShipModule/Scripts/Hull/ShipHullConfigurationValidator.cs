using System;
using Zenject;

namespace Features.ShipModule.Scripts.Hull {
    // The one startup check of the hull configuration: armor never heals and never takes off all the damage.
    public sealed class ShipHullConfigurationValidator : IInitializable {
        private readonly ShipHullConfiguration _shipHullConfiguration;

        public ShipHullConfigurationValidator(ShipHullConfiguration shipHullConfiguration) =>
            _shipHullConfiguration = shipHullConfiguration;

        public void Initialize() {
            if (_shipHullConfiguration.DamageReductionPerArmor < 0f)
                throw new InvalidOperationException($"{_shipHullConfiguration.name}: {nameof(ShipHullConfiguration.DamageReductionPerArmor)} must be 0 or above, is {_shipHullConfiguration.DamageReductionPerArmor}.");

            float maxReduction = _shipHullConfiguration.MaxArmorReduction;
            if (maxReduction < 0f || maxReduction >= 1f)
                throw new InvalidOperationException($"{_shipHullConfiguration.name}: {nameof(ShipHullConfiguration.MaxArmorReduction)} must be in [0, 1), is {maxReduction}.");

            if (_shipHullConfiguration.DebugDamage <= 0f)
                throw new InvalidOperationException($"{_shipHullConfiguration.name}: {nameof(ShipHullConfiguration.DebugDamage)} must be above 0, is {_shipHullConfiguration.DebugDamage}.");
        }
    }
}
