using System;
using Zenject;

namespace Features.ShipImpactModule.Scripts {
    // The one startup check of the rock impact configuration.
    public sealed class ShipRockImpactConfigurationValidator : IInitializable {
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        public ShipRockImpactConfigurationValidator(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public void Initialize() {
            ShipRockImpactConfiguration configuration = _shipRockImpactConfiguration;
            if (configuration.SmallDamageMaxDiameter <= configuration.SmallDamageMinDiameter)
                throw new InvalidOperationException($"{configuration.name}: {nameof(ShipRockImpactConfiguration.SmallDamageMaxDiameter)} must be above {nameof(ShipRockImpactConfiguration.SmallDamageMinDiameter)}.");

            if (configuration.MaxSpeedFactor < configuration.MinSpeedFactor)
                throw new InvalidOperationException($"{configuration.name}: {nameof(ShipRockImpactConfiguration.MaxSpeedFactor)} must not be below {nameof(ShipRockImpactConfiguration.MinSpeedFactor)}.");

            if (configuration.ImpactEffect == null)
                throw new InvalidOperationException($"{configuration.name}: {nameof(ShipRockImpactConfiguration.ImpactEffect)} is not set.");

            RequireAtLeastZero(configuration.SmallDamageAtMinDiameter, nameof(ShipRockImpactConfiguration.SmallDamageAtMinDiameter));
            RequireAtLeastZero(configuration.SmallDamageAtMaxDiameter, nameof(ShipRockImpactConfiguration.SmallDamageAtMaxDiameter));
            RequireAtLeastZero(configuration.MinSpeedFactor, nameof(ShipRockImpactConfiguration.MinSpeedFactor));
            RequireAtLeastZero(configuration.LargeRockDamage, nameof(ShipRockImpactConfiguration.LargeRockDamage));
            RequireAtLeastZero(configuration.LargeContactMarginMeters, nameof(ShipRockImpactConfiguration.LargeContactMarginMeters));
            RequireAtLeastZero(configuration.BrakingLookAheadShare, nameof(ShipRockImpactConfiguration.BrakingLookAheadShare));
            RequireAtLeastZero(configuration.MaxLookAheadMeters, nameof(ShipRockImpactConfiguration.MaxLookAheadMeters));
            RequireAtLeastZero(configuration.LargeHitCooldownSeconds, nameof(ShipRockImpactConfiguration.LargeHitCooldownSeconds));
            RequireAboveZero(configuration.ReferenceSpeed, nameof(ShipRockImpactConfiguration.ReferenceSpeed));
            RequireAboveZero(configuration.SmallPushSeconds, nameof(ShipRockImpactConfiguration.SmallPushSeconds));
            RequireAboveZero(configuration.SmallMaxAcceleration, nameof(ShipRockImpactConfiguration.SmallMaxAcceleration));
            RequireAboveZero(configuration.LargeKnockbackSeconds, nameof(ShipRockImpactConfiguration.LargeKnockbackSeconds));
            RequireAboveZero(configuration.LargeMaxAcceleration, nameof(ShipRockImpactConfiguration.LargeMaxAcceleration));
            RequireAboveZero(configuration.ContactPushOutSpeed, nameof(ShipRockImpactConfiguration.ContactPushOutSpeed));
            RequireAboveZero(configuration.SmallContactRadiusShare, nameof(ShipRockImpactConfiguration.SmallContactRadiusShare));
            RequireAboveZero(configuration.LargeContactRadiusShare, nameof(ShipRockImpactConfiguration.LargeContactRadiusShare));
            RequireAboveZero(configuration.MaxSweepMeters, nameof(ShipRockImpactConfiguration.MaxSweepMeters));
            RequireShake(configuration.SmallFeel, nameof(ShipRockImpactConfiguration.SmallFeel));
            RequireShake(configuration.LargeFeel, nameof(ShipRockImpactConfiguration.LargeFeel));
        }

        private void RequireShake(ShipRockImpactFeel feel, string field) {
            if (feel.Shake == null)
                throw new InvalidOperationException($"{_shipRockImpactConfiguration.name}: {field} has no camera shake profile.");

            RequireAboveZero(feel.Shake.Seconds, field);
        }

        private void RequireAtLeastZero(float value, string field) {
            if (value < 0f)
                throw new InvalidOperationException($"{_shipRockImpactConfiguration.name}: {field} must be 0 or above, is {value}.");
        }

        private void RequireAboveZero(float value, string field) {
            if (value <= 0f)
                throw new InvalidOperationException($"{_shipRockImpactConfiguration.name}: {field} must be above 0, is {value}.");
        }
    }
}
