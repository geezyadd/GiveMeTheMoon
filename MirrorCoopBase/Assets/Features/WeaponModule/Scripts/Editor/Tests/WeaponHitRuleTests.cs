using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Hits;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class WeaponHitRuleTests {
        private const string WEAPON_ID = "minigun";
        private const string OTHER_WEAPON_ID = "harpoon";
        private const float SHOTS_PER_SECOND = 20f;
        private const float RANGE_METERS = 400f;
        private const float FIRE_RATE_TOLERANCE = 0.25f;
        private const float BURST_SECONDS = 1f;
        private const float RANGE_TOLERANCE_METERS = 25f;
        private const float IN_RANGE_DISTANCE_METERS = 100f;
        private const double START_SECONDS = 100d;

        private static readonly WeaponHitLimits _limits = new WeaponHitLimits(SHOTS_PER_SECOND, RANGE_METERS, maxHitsPerShot: 1);

        private WeaponHitRuleConfiguration _configuration;
        private WeaponHitRule _rule;
        private WeaponShotBudget _budget;

        [SetUp]
        public void SetUp() {
            _configuration = CreateConfiguration();
            _rule = new WeaponHitRule(_configuration);
            _budget = new WeaponShotBudget();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenShooterHoldsThisWeaponAndAllIsFine_ThenHitIsAccepted() =>
            Assert.AreEqual(WeaponHitVerdict.Accepted, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: true, START_SECONDS));

        [Test]
        public void WhenShooterHoldsAnotherWeapon_ThenHitIsRejected() =>
            Assert.AreEqual(WeaponHitVerdict.WeaponNotHeld, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), OTHER_WEAPON_ID, isTargetAlive: true, START_SECONDS));

        [Test]
        public void WhenShooterHoldsNoWeapon_ThenHitIsRejected() =>
            Assert.AreEqual(WeaponHitVerdict.WeaponNotHeld, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), string.Empty, isTargetAlive: true, START_SECONDS));

        [Test]
        public void WhenTargetIsNoLongerAlive_ThenHitIsRejected() =>
            Assert.AreEqual(WeaponHitVerdict.TargetNotAlive, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: false, START_SECONDS));

        [Test]
        public void WhenHitIsBeyondRangeWithinTolerance_ThenHitIsAccepted() =>
            Assert.AreEqual(WeaponHitVerdict.Accepted, Evaluate(CreateHit(WEAPON_ID, RANGE_METERS + RANGE_TOLERANCE_METERS - 1f), WEAPON_ID, isTargetAlive: true, START_SECONDS));

        [Test]
        public void WhenHitIsBeyondRangeAndTolerance_ThenHitIsRejected() =>
            Assert.AreEqual(WeaponHitVerdict.OutOfRange, Evaluate(CreateHit(WEAPON_ID, RANGE_METERS + RANGE_TOLERANCE_METERS + 1f), WEAPON_ID, isTargetAlive: true, START_SECONDS));

        [Test]
        public void WhenHitsComeAtTheFireRateForLong_ThenEveryHitIsAccepted() {
            int rejected = ReportStream(hitsPerSecond: SHOTS_PER_SECOND, seconds: 30f);

            Assert.AreEqual(0, rejected);
        }

        [Test]
        public void WhenHitsComeSlightlyFasterWithinTolerance_ThenEveryHitIsAccepted() {
            int rejected = ReportStream(hitsPerSecond: SHOTS_PER_SECOND * (1f + FIRE_RATE_TOLERANCE * 0.8f), seconds: 30f);

            Assert.AreEqual(0, rejected);
        }

        [Test]
        public void WhenHitsComeTwiceTheFireRate_ThenTheExtraHitsAreRejected() {
            float seconds = 10f;
            int rejected = ReportStream(hitsPerSecond: SHOTS_PER_SECOND * 2f, seconds);

            float allowed = SHOTS_PER_SECOND * (1f + FIRE_RATE_TOLERANCE) * (seconds + BURST_SECONDS);
            Assert.That(SHOTS_PER_SECOND * 2f * seconds - rejected, Is.LessThanOrEqualTo(allowed + 1f));
            Assert.That(rejected, Is.GreaterThan(0));
        }

        [Test]
        public void WhenABatchArrivesAfterAHitch_ThenHitsUpToTheBurstAreAccepted() {
            int burst = Mathf.FloorToInt(SHOTS_PER_SECOND * (1f + FIRE_RATE_TOLERANCE) * BURST_SECONDS);

            for (int i = 0; i < burst; i++)
                Assert.AreEqual(WeaponHitVerdict.Accepted, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: true, START_SECONDS), $"hit {i}");

            Assert.AreEqual(WeaponHitVerdict.FireRateExceeded, Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: true, START_SECONDS));
        }

        [Test]
        public void WhenARejectedHitIsReported_ThenTheBudgetIsNotSpent() {
            Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: true, START_SECONDS);
            float available = _budget.AvailableHits;

            Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: false, START_SECONDS);

            Assert.AreEqual(available, _budget.AvailableHits);
        }

        private int ReportStream(float hitsPerSecond, float seconds) {
            int rejected = 0;
            int count = Mathf.FloorToInt(hitsPerSecond * seconds);
            for (int i = 0; i < count; i++) {
                double now = START_SECONDS + i / (double)hitsPerSecond;
                if (Evaluate(CreateHit(WEAPON_ID, IN_RANGE_DISTANCE_METERS), WEAPON_ID, isTargetAlive: true, now) == WeaponHitVerdict.FireRateExceeded)
                    rejected++;
            }

            return rejected;
        }

        private WeaponHitVerdict Evaluate(WeaponHit hit, string heldWeaponId, bool isTargetAlive, double serverTimeSeconds) =>
            _rule.Evaluate(hit, new WeaponHitCircumstances(heldWeaponId, Vector3.zero, isTargetAlive, serverTimeSeconds), _limits, _budget);

        private static WeaponHit CreateHit(string weaponId, float distanceMeters) =>
            new WeaponHit(targetId: 7, new Vector3(0f, 0f, distanceMeters), Vector3.back, damage: 1f, new PlayerKey("client:shooter"), weaponId);

        private static WeaponHitRuleConfiguration CreateConfiguration() {
            WeaponHitRuleConfiguration configuration = ScriptableObject.CreateInstance<WeaponHitRuleConfiguration>();
            SerializedObject serialized = new SerializedObject(configuration);
            serialized.FindProperty("_fireRateTolerance").floatValue = FIRE_RATE_TOLERANCE;
            serialized.FindProperty("_burstSeconds").floatValue = BURST_SECONDS;
            serialized.FindProperty("_rangeToleranceMeters").floatValue = RANGE_TOLERANCE_METERS;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return configuration;
        }
    }
}
