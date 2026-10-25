using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // Hull damage of a rock hit before armor (the hull service applies armor): a small rock deals more the bigger it
    // is (linear between the configured diameters) and the faster the ship hits it (linear in speed, clamped).
    public sealed class ShipRockDamageRule : IShipRockDamageRule {
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        public ShipRockDamageRule(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public float CalculateSmallRockDamage(float rockDiameter, float shipSpeed) {
            float sizeShare = Mathf.InverseLerp(_shipRockImpactConfiguration.SmallDamageMinDiameter, _shipRockImpactConfiguration.SmallDamageMaxDiameter, rockDiameter);
            float damage = Mathf.Lerp(_shipRockImpactConfiguration.SmallDamageAtMinDiameter, _shipRockImpactConfiguration.SmallDamageAtMaxDiameter, sizeShare);
            return damage * CalculateSpeedFactor(shipSpeed);
        }

        public float CalculateLargeRockDamage() =>
            _shipRockImpactConfiguration.LargeRockDamage;

        public float CalculateStrength(float shipSpeed) =>
            Mathf.Clamp01(shipSpeed / _shipRockImpactConfiguration.ReferenceSpeed);

        private float CalculateSpeedFactor(float shipSpeed) =>
            Mathf.Clamp(shipSpeed / _shipRockImpactConfiguration.ReferenceSpeed, _shipRockImpactConfiguration.MinSpeedFactor, _shipRockImpactConfiguration.MaxSpeedFactor);
    }
}
