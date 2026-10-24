using Features.ShipModule.Scripts.Damage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipHullLeakRuleTests {
        private const float LEAK_PER_SECOND = 0.5f;
        private const float LEAK_STEP = 1f;
        private const float TOLERANCE = 0.0001f;

        private ShipDamageConfiguration _configuration;
        private ShipHullLeakRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipDamageConfiguration>();
            SerializedObject serialized = new SerializedObject(_configuration);
            serialized.FindProperty("_leakHullPerSecond").floatValue = LEAK_PER_SECOND;
            serialized.FindProperty("_leakStepHull").floatValue = LEAK_STEP;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ShipHullLeakRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenLeakIsBelowTheStep_ThenItIsGatheredAndNothingIsDealt() {
            ShipHullLeakStep step = _rule.Advance(0f, 1, 1f);

            Assert.AreEqual(LEAK_PER_SECOND, step.PendingLeak, TOLERANCE);
            Assert.AreEqual(0f, step.DealtLeak, TOLERANCE);
        }

        [Test]
        public void WhenGatheredLeakReachesTheStep_ThenAllOfItIsDealtAndThePendingIsEmpty() {
            ShipHullLeakStep step = _rule.Advance(0.75f, 1, 1f);

            Assert.AreEqual(1.25f, step.DealtLeak, TOLERANCE);
            Assert.AreEqual(0f, step.PendingLeak, TOLERANCE);
        }

        [Test]
        public void WhenSeveralBreachesAreOpen_ThenEachLeaks() {
            ShipHullLeakStep step = _rule.Advance(0f, 3, 1f);

            Assert.AreEqual(3 * LEAK_PER_SECOND, step.DealtLeak, TOLERANCE);
        }

        [Test]
        public void WhenNoBreachIsOpen_ThenNothingLeaksAndThePendingIsDropped() {
            ShipHullLeakStep step = _rule.Advance(0.9f, 0, 1f);

            Assert.AreEqual(0f, step.PendingLeak, TOLERANCE);
            Assert.AreEqual(0f, step.DealtLeak, TOLERANCE);
        }
    }
}
