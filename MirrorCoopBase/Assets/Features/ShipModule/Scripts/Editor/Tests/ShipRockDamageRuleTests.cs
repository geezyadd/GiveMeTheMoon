using Features.ShipModule.Scripts.Hull;
using Features.ShipModule.Scripts.Impacts;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipRockDamageRuleTests {
        private const float TOLERANCE = 0.0001f;
        private const float CRUISE_SPEED = 14f;
        private const float DEBRIS_ROCK_MIN_DIAMETER = 10f;
        private const float DEBRIS_ROCK_MAX_DIAMETER = 15f;
        private const float CLUSTER_SMALL_ROCK_MIN_DIAMETER = 25f;
        private const float CLUSTER_SMALL_ROCK_MAX_DIAMETER = 35f;
        private const float ARMOR = 30f;

        private ShipRockImpactConfiguration _configuration;
        private ShipHullConfiguration _hullConfiguration;
        private ShipRockDamageRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipRockImpactConfiguration>();
            _hullConfiguration = ScriptableObject.CreateInstance<ShipHullConfiguration>();
            _rule = new ShipRockDamageRule(_configuration);
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_configuration);
            Object.DestroyImmediate(_hullConfiguration);
        }

        [TestCase(DEBRIS_ROCK_MIN_DIAMETER, 3f, 5f)]
        [TestCase(DEBRIS_ROCK_MAX_DIAMETER, 3f, 5f)]
        [TestCase(CLUSTER_SMALL_ROCK_MIN_DIAMETER, 8f, 12f)]
        [TestCase(CLUSTER_SMALL_ROCK_MAX_DIAMETER, 8f, 12f)]
        public void WhenSmallRockIsHitAtCruiseSpeed_ThenTheDamageIsInItsStartRange(float diameter, float min, float max) {
            float damage = _rule.CalculateSmallRockDamage(diameter, CRUISE_SPEED);

            Assert.GreaterOrEqual(damage, min, $"A {diameter} m rock deals too little.");
            Assert.LessOrEqual(damage, max, $"A {diameter} m rock deals too much.");
        }

        [Test]
        public void WhenTheShipIsFaster_ThenTheSameRockDealsMore() =>
            Assert.Greater(_rule.CalculateSmallRockDamage(DEBRIS_ROCK_MAX_DIAMETER, CRUISE_SPEED * 1.5f), _rule.CalculateSmallRockDamage(DEBRIS_ROCK_MAX_DIAMETER, CRUISE_SPEED));

        [Test]
        public void WhenTheSpeedIsFarOutside_ThenTheSpeedFactorIsClamped() {
            float atCruise = _rule.CalculateSmallRockDamage(DEBRIS_ROCK_MIN_DIAMETER, CRUISE_SPEED);

            Assert.AreEqual(atCruise * _configuration.MinSpeedFactor, _rule.CalculateSmallRockDamage(DEBRIS_ROCK_MIN_DIAMETER, 0f), TOLERANCE, "A resting ship takes no clamped damage.");
            Assert.AreEqual(atCruise * _configuration.MaxSpeedFactor, _rule.CalculateSmallRockDamage(DEBRIS_ROCK_MIN_DIAMETER, CRUISE_SPEED * 100f), TOLERANCE, "A very fast ship takes unclamped damage.");
        }

        [Test]
        public void WhenLargeRockIsHit_ThenItDealsNoDamageByDefault() =>
            Assert.AreEqual(0f, _rule.CalculateLargeRockDamage(), TOLERANCE);

        [Test]
        public void WhenTheShipHasArmor_ThenTheHullTakesLessOfTheRockDamage() {
            float damage = _rule.CalculateSmallRockDamage(CLUSTER_SMALL_ROCK_MAX_DIAMETER, CRUISE_SPEED);

            float dealt = new ShipHullDamageRule(_hullConfiguration).CalculateDealtDamage(damage, ARMOR);

            Assert.AreEqual(damage * (1f - ARMOR * _hullConfiguration.DamageReductionPerArmor), dealt, TOLERANCE);
        }

        [Test]
        public void WhenTheHitIsSlow_ThenItIsFeltWeaker() {
            Assert.AreEqual(1f, _rule.CalculateStrength(CRUISE_SPEED), TOLERANCE, "A hit at cruise speed is not felt at full strength.");
            Assert.AreEqual(0.5f, _rule.CalculateStrength(CRUISE_SPEED * 0.5f), TOLERANCE);
            Assert.AreEqual(1f, _rule.CalculateStrength(CRUISE_SPEED * 3f), TOLERANCE, "The strength is not capped at 1.");
        }
    }
}
