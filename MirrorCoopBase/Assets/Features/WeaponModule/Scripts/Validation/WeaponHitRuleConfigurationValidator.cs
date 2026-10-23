using System;
using Features.WeaponModule.Scripts.Definitions;
using Zenject;

namespace Features.WeaponModule.Scripts.Validation {
    // Fails startup when the hit rule could never accept a hit.
    public sealed class WeaponHitRuleConfigurationValidator : IInitializable {
        private readonly WeaponHitRuleConfiguration _weaponHitRuleConfiguration;

        public WeaponHitRuleConfigurationValidator(WeaponHitRuleConfiguration weaponHitRuleConfiguration) =>
            _weaponHitRuleConfiguration = weaponHitRuleConfiguration;

        public void Initialize() {
            if (_weaponHitRuleConfiguration.BurstSeconds <= 0f)
                throw new InvalidOperationException($"{_weaponHitRuleConfiguration.name}: burst seconds must be above 0, or no hit fits the budget.");
        }
    }
}
