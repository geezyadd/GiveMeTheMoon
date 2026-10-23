using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Hits {
    public interface IWeaponHitRule {
        // Spends one hit of the budget only when the hit is accepted.
        public WeaponHitVerdict Evaluate(in WeaponHit hit, in WeaponHitCircumstances circumstances, in WeaponHitLimits limits, WeaponShotBudget budget);
    }
}
