using System.Collections.Generic;
using Features.ShipDamageModule.Scripts.Damage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Editor.Tests {
    public sealed class ShipDamageRuleTests {
        private const float SEARCH_RADIUS = 6f;
        private const float MODULE_BREAK_CHANCE = 0.5f;
        private const float BREACH_CHANCE = 0.6f;
        private const int MAX_OPEN_BREACHES = 2;
        private const float FULL_STRENGTH = 1f;
        private const float HALF_STRENGTH = 0.5f;
        private const float SURE_ROLL = 0f;
        private const float HIGHEST_ROLL = 0.9999f;
        private const string NEAR_SOCKET = "engine-left";
        private const string FAR_SOCKET = "radar";
        private const string HELM_SOCKET = "control";
        private const string NEAR_POINT = "breach-01";
        private const string FAR_POINT = "breach-02";

        private static readonly Vector3 _hit = Vector3.zero;

        private ShipDamageConfiguration _configuration;
        private ShipDamageRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipDamageConfiguration>();
            SerializedObject serialized = new SerializedObject(_configuration);
            serialized.FindProperty("_moduleSearchRadius").floatValue = SEARCH_RADIUS;
            serialized.FindProperty("_moduleBreakChance").floatValue = MODULE_BREAK_CHANCE;
            serialized.FindProperty("_breachChance").floatValue = BREACH_CHANCE;
            serialized.FindProperty("_maxOpenBreaches").intValue = MAX_OPEN_BREACHES;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ShipDamageRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenImpactHitsBetweenModules_AndRollIsSure_ThenTheNearestModuleBreaks() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> {
                Module(FAR_SOCKET, 4f, true),
                Module(NEAR_SOCKET, 1f, true)
            };

            Assert.IsTrue(_rule.TryPickBrokenModule(modules, _hit, FULL_STRENGTH, SURE_ROLL, out string socketId));
            Assert.AreEqual(NEAR_SOCKET, socketId);
        }

        [Test]
        public void WhenOnlyModuleIsOutsideTheRadius_ThenNothingBreaks() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> { Module(FAR_SOCKET, SEARCH_RADIUS + 1f, true) };

            Assert.IsFalse(_rule.TryPickBrokenModule(modules, _hit, FULL_STRENGTH, SURE_ROLL, out _));
        }

        [Test]
        public void WhenNearestModuleCannotBreak_ThenTheNextNearestBreaks() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> {
                Module(HELM_SOCKET, 0.5f, false),
                Module(FAR_SOCKET, 3f, true)
            };

            Assert.IsTrue(_rule.TryPickBrokenModule(modules, _hit, FULL_STRENGTH, SURE_ROLL, out string socketId));
            Assert.AreEqual(FAR_SOCKET, socketId);
        }

        [Test]
        public void WhenNoModuleCanBreak_ThenNothingBreaksWhateverTheRoll() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> { Module(HELM_SOCKET, 0.5f, false) };

            Assert.IsFalse(_rule.TryPickBrokenModule(modules, _hit, FULL_STRENGTH, SURE_ROLL, out _));
        }

        [Test]
        public void WhenRollIsAboveTheChance_ThenNothingBreaks_AndBelowItBreaks() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> { Module(NEAR_SOCKET, 1f, true) };
            float chance = MODULE_BREAK_CHANCE * HALF_STRENGTH;

            Assert.IsFalse(_rule.TryPickBrokenModule(modules, _hit, HALF_STRENGTH, chance + 0.01f, out _), "A roll above chance x strength broke the module.");
            Assert.IsTrue(_rule.TryPickBrokenModule(modules, _hit, HALF_STRENGTH, chance - 0.01f, out _), "A roll below chance x strength did not break the module.");
        }

        [Test]
        public void WhenModuleHasAChanceMultiplier_ThenItScalesTheChance() {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate> {
                new ShipDamageCandidate(NEAR_SOCKET, Vector3.forward, true, 2f)
            };
            float roll = MODULE_BREAK_CHANCE * HALF_STRENGTH + 0.1f;

            Assert.IsTrue(_rule.TryPickBrokenModule(modules, _hit, HALF_STRENGTH, roll, out _), "A multiplier of 2 did not double the chance.");
            Assert.IsTrue(_rule.TryPickBrokenModule(modules, _hit, FULL_STRENGTH, HIGHEST_ROLL, out _), "A chance of 1 and more did not always break.");
        }

        [Test]
        public void WhenImpactOpensABreach_ThenItIsTheNearestFreePoint() {
            List<ShipDamageCandidate> points = new List<ShipDamageCandidate> {
                Point(NEAR_POINT, 1f, false),
                Point(FAR_POINT, 20f, true)
            };

            Assert.IsTrue(_rule.TryPickBreach(points, 1, _hit, FULL_STRENGTH, SURE_ROLL, out string pointId));
            Assert.AreEqual(FAR_POINT, pointId, "An open point was opened again instead of the nearest free one.");
        }

        [Test]
        public void WhenBreachLimitIsReached_ThenNoBreachOpens() {
            List<ShipDamageCandidate> points = new List<ShipDamageCandidate> { Point(NEAR_POINT, 1f, true) };

            Assert.IsFalse(_rule.TryPickBreach(points, MAX_OPEN_BREACHES, _hit, FULL_STRENGTH, SURE_ROLL, out _));
            Assert.IsTrue(_rule.TryPickBreach(points, MAX_OPEN_BREACHES - 1, _hit, FULL_STRENGTH, SURE_ROLL, out _));
        }

        [Test]
        public void WhenBreachRollIsAboveTheChance_ThenNoBreachOpens() {
            List<ShipDamageCandidate> points = new List<ShipDamageCandidate> { Point(NEAR_POINT, 1f, true) };

            Assert.IsFalse(_rule.TryPickBreach(points, 0, _hit, FULL_STRENGTH, BREACH_CHANCE + 0.01f, out _));
        }

        private static ShipDamageCandidate Module(string socketId, float distance, bool canBreak) =>
            new ShipDamageCandidate(socketId, Vector3.forward * distance, canBreak, 1f);

        private static ShipDamageCandidate Point(string pointId, float distance, bool isFree) =>
            new ShipDamageCandidate(pointId, Vector3.right * distance, isFree, 1f);
    }
}
