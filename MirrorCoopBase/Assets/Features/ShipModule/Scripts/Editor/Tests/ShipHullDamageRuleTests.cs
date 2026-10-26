using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipHullDamageRuleTests {
        private const float TOLERANCE = 0.0001f;
        private const float HIT = 20f;

        private ShipHullConfiguration _configuration;
        private ShipHullDamageRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipHullConfiguration>();
            _rule = new ShipHullDamageRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenShipHasNoArmor_ThenTheWholeHitIsDealt() =>
            Assert.AreEqual(HIT, _rule.CalculateDealtDamage(HIT, 0f), TOLERANCE);

        [Test]
        public void WhenShipHasArmor_ThenEachPointTakesOffItsShare() {
            const float armor = 30f;
            float expected = HIT * (1f - armor * _configuration.DamageReductionPerArmor);

            Assert.AreEqual(expected, _rule.CalculateDealtDamage(HIT, armor), TOLERANCE);
        }

        [Test]
        public void WhenArmorIsAboveTheCap_ThenTheReductionStopsAtTheCap() {
            const float armor = 1000f;
            float expected = HIT * (1f - _configuration.MaxArmorReduction);

            Assert.AreEqual(expected, _rule.CalculateDealtDamage(HIT, armor), TOLERANCE);
        }

        [Test]
        public void WhenHitIsNegative_ThenNothingIsDealt() =>
            Assert.AreEqual(0f, _rule.CalculateDealtDamage(-HIT, 0f), TOLERANCE);
    }
}
