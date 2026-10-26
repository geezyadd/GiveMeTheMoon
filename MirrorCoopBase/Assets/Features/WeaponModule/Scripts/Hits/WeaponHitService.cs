using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Targets;

namespace Features.WeaponModule.Scripts.Hits {
    // The server end of every weapon's hit path: a reported hit on a known target passes the hit rule, then each hit
    // effect of the held weapon is applied to the target. Anything else is dropped without a word.
    public sealed class WeaponHitService : IWeaponHitService {
        private readonly IWeaponHitRule _weaponHitRule;
        private readonly IWeaponTargetLookup _weaponTargetLookup;
        private readonly WeaponCatalog _weaponCatalog;

        public WeaponHitService(IWeaponHitRule weaponHitRule, IWeaponTargetLookup weaponTargetLookup, WeaponCatalog weaponCatalog) {
            _weaponHitRule = weaponHitRule;
            _weaponTargetLookup = weaponTargetLookup;
            _weaponCatalog = weaponCatalog;
        }

        public WeaponHitVerdict ServerApplyReportedHit(ServerWeaponHolding holding, int reportedWeaponIndex, in WeaponHitReport report, double serverTimeSeconds) {
            if (reportedWeaponIndex < 0 || reportedWeaponIndex >= _weaponCatalog.Weapons.Count)
                return WeaponHitVerdict.WeaponNotHeld;

            if (_weaponTargetLookup.TryGet(report.TargetId, out IWeaponTarget target) == false)
                return WeaponHitVerdict.UnknownTarget;

            WeaponDefinition held = holding.Weapon;
            WeaponHit hit = new WeaponHit(report.TargetId, report.Point, report.Normal, held.DamagePerHit, holding.Shooter, _weaponCatalog.Weapons[reportedWeaponIndex].Id);
            WeaponHitCircumstances circumstances = new WeaponHitCircumstances(held.Id, holding.Aim.Origin, target.IsAlive, serverTimeSeconds);
            WeaponHitVerdict verdict = _weaponHitRule.Evaluate(hit, circumstances, WeaponHitLimits.CreateFor(held), holding.Budget);
            if (verdict == WeaponHitVerdict.Accepted)
                ApplyEffects(held, hit, target);

            return verdict;
        }

        private static void ApplyEffects(WeaponDefinition weapon, in WeaponHit hit, IWeaponTarget target) {
            for (int i = 0; i < weapon.HitEffects.Count; i++)
                weapon.HitEffects[i].Apply(hit, target);
        }
    }
}
