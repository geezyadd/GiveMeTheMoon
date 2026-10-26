using System;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Hits {
    // The server's check of a hit a shooting client reported: the shooter holds this weapon, the target is still
    // alive, the hit lies within range plus tolerance, and the hit rate stays within the weapon's budget. The budget
    // is a token bucket: it refills at the weapon's hit rate plus the tolerance and holds at most BurstSeconds of
    // hits, so a batch after a network hitch passes and a sustained faster stream does not.
    public sealed class WeaponHitRule : IWeaponHitRule {
        private const float HIT_COST = 1f;

        private readonly WeaponHitRuleConfiguration _weaponHitRuleConfiguration;

        public WeaponHitRule(WeaponHitRuleConfiguration weaponHitRuleConfiguration) =>
            _weaponHitRuleConfiguration = weaponHitRuleConfiguration;

        public WeaponHitVerdict Evaluate(in WeaponHit hit, in WeaponHitCircumstances circumstances, in WeaponHitLimits limits, WeaponShotBudget budget) {
            if (string.Equals(hit.WeaponId, circumstances.HeldWeaponId, StringComparison.Ordinal) == false)
                return WeaponHitVerdict.WeaponNotHeld;

            if (circumstances.IsTargetAlive == false)
                return WeaponHitVerdict.TargetNotAlive;

            if (IsOutOfRange(hit, circumstances, limits))
                return WeaponHitVerdict.OutOfRange;

            float availableHits = CountAvailableHits(limits, budget, circumstances.ServerTimeSeconds);
            if (availableHits < HIT_COST) {
                budget.Recount(availableHits, circumstances.ServerTimeSeconds);
                return WeaponHitVerdict.FireRateExceeded;
            }

            budget.Recount(availableHits - HIT_COST, circumstances.ServerTimeSeconds);
            return WeaponHitVerdict.Accepted;
        }

        private bool IsOutOfRange(in WeaponHit hit, in WeaponHitCircumstances circumstances, in WeaponHitLimits limits) {
            float allowedMeters = limits.RangeMeters + _weaponHitRuleConfiguration.RangeToleranceMeters;
            return (hit.Point - circumstances.ShooterPosition).sqrMagnitude > allowedMeters * allowedMeters;
        }

        private float CountAvailableHits(in WeaponHitLimits limits, WeaponShotBudget budget, double nowSeconds) {
            float hitsPerSecond = limits.ShotsPerSecond * limits.MaxHitsPerShot * (1f + _weaponHitRuleConfiguration.FireRateTolerance);
            float capacity = Mathf.Max(HIT_COST, hitsPerSecond * _weaponHitRuleConfiguration.BurstSeconds);
            if (budget.IsStarted == false)
                return capacity;

            float elapsedSeconds = (float)Math.Max(0d, nowSeconds - budget.CountedAtSeconds);
            return Mathf.Min(capacity, budget.AvailableHits + elapsedSeconds * hitsPerSecond);
        }
    }
}
