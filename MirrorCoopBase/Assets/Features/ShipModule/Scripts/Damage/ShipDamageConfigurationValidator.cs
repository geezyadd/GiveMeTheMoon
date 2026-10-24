using System;
using Zenject;

namespace Features.ShipModule.Scripts.Damage {
    // The one startup check of the damage configuration: a repair takes time, a breach neither leaks nor heals negative HP.
    public sealed class ShipDamageConfigurationValidator : IInitializable {
        private readonly ShipDamageConfiguration _shipDamageConfiguration;

        public ShipDamageConfigurationValidator(ShipDamageConfiguration shipDamageConfiguration) =>
            _shipDamageConfiguration = shipDamageConfiguration;

        public void Initialize() {
            if (_shipDamageConfiguration.RepairSeconds <= 0f)
                throw new InvalidOperationException($"{_shipDamageConfiguration.name}: {nameof(ShipDamageConfiguration.RepairSeconds)} must be above 0, is {_shipDamageConfiguration.RepairSeconds}.");

            if (_shipDamageConfiguration.MaxOpenBreaches < 0)
                throw new InvalidOperationException($"{_shipDamageConfiguration.name}: {nameof(ShipDamageConfiguration.MaxOpenBreaches)} must be 0 or above, is {_shipDamageConfiguration.MaxOpenBreaches}.");

            if (_shipDamageConfiguration.LeakHullPerSecond < 0f || _shipDamageConfiguration.HullPerRepairedBreach < 0f)
                throw new InvalidOperationException($"{_shipDamageConfiguration.name}: {nameof(ShipDamageConfiguration.LeakHullPerSecond)} and {nameof(ShipDamageConfiguration.HullPerRepairedBreach)} must be 0 or above.");
        }
    }
}
