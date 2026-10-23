using UnityEngine;

namespace Features.ShipModule.Scripts.Hull {
    // Armor takes off a share of every hit: armor x reduction per point, capped by the configured maximum.
    public sealed class ShipHullDamageRule : IShipHullDamageRule {
        private readonly ShipHullConfiguration _shipHullConfiguration;

        public ShipHullDamageRule(ShipHullConfiguration shipHullConfiguration) =>
            _shipHullConfiguration = shipHullConfiguration;

        public float CalculateDealtDamage(float amount, float armor) {
            float reduction = Mathf.Clamp(
                armor * _shipHullConfiguration.DamageReductionPerArmor,
                0f,
                _shipHullConfiguration.MaxArmorReduction);
            return Mathf.Max(0f, amount) * (1f - reduction);
        }
    }
}
