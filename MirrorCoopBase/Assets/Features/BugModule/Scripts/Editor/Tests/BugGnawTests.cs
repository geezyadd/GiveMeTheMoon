using System.Collections.Generic;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Gnaw;
using Features.GameCoreModule.Contracts;
using Features.ShipDamageModule.Scripts.Damage;
using NUnit.Framework;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // A bite goes through the ship's own damage rules: hull HP with cause Gnaw, and a weak impact that may break the
    // module beside the bug or open a breach.
    public sealed class BugGnawTests {
        private const int BUG_ID = 7;
        private const string ENGINE_SOCKET = "engine-left";
        private const string HULL_POINT = "breach-01";
        // Under every bite chance of the default numbers: the rule breaks or opens what is in reach.
        private const float SURE_ROLL = 0f;
        private const float HIGHEST_ROLL = 0.9999f;
        private const float ROLL_MARGIN = 0.001f;
        // A bite is a nibble: well under half a full rock impact.
        private const float MAX_BITE_SHARE_OF_ROCK = 0.5f;

        private static readonly Vector3 _engine = new(2f, 0f, 1f);

        private BugConfiguration _bugConfiguration;
        private ShipDamageConfiguration _shipDamageConfiguration;
        private RecordingShipHull _hull;

        [SetUp]
        public void SetUp() {
            _bugConfiguration = ScriptableObject.CreateInstance<BugConfiguration>();
            _shipDamageConfiguration = ScriptableObject.CreateInstance<ShipDamageConfiguration>();
            _hull = new RecordingShipHull();
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_bugConfiguration);
            Object.DestroyImmediate(_shipDamageConfiguration);
        }

        [Test]
        public void WhenABugBites_ThenTheHullLosesTheBiteHpWithCauseGnawFromThatBug() {
            RuleImpactDamageService impacts = CreateImpacts(HIGHEST_ROLL);
            BugGnawService gnaw = new(_hull, impacts, _bugConfiguration);

            gnaw.ServerBite(BUG_ID, _engine);

            Assert.AreEqual(1, _hull.Damages.Count);
            ShipDamage damage = _hull.Damages[0];
            Assert.AreEqual(_bugConfiguration.BiteHull, damage.Amount);
            Assert.AreEqual(ShipDamageCause.Gnaw, damage.Cause);
            Assert.AreEqual(BUG_ID, damage.SourceId);
            Assert.AreEqual(_engine, damage.HitPoint);
        }

        [Test]
        public void WhenABugBitesBesideAModule_AndTheRuleRollsUnderTheBiteChance_ThenTheModuleBreaksAndABreachOpens() {
            RuleImpactDamageService impacts = CreateImpacts(SURE_ROLL);
            BugGnawService gnaw = new(_hull, impacts, _bugConfiguration);

            gnaw.ServerBite(BUG_ID, _engine);

            CollectionAssert.AreEqual(new[] { _bugConfiguration.BiteStrength }, impacts.Strengths);
            CollectionAssert.AreEqual(new[] { ENGINE_SOCKET }, impacts.BrokenSockets);
            CollectionAssert.AreEqual(new[] { HULL_POINT }, impacts.OpenedBreaches);
        }

        [Test]
        public void WhenABugBites_AndTheRuleRollsAboveTheBiteChance_ThenOnlyTheHullLosesHp() {
            RuleImpactDamageService impacts = CreateImpacts(HIGHEST_ROLL);
            BugGnawService gnaw = new(_hull, impacts, _bugConfiguration);

            gnaw.ServerBite(BUG_ID, _engine);

            Assert.AreEqual(1, _hull.Damages.Count);
            Assert.IsEmpty(impacts.BrokenSockets);
            Assert.IsEmpty(impacts.OpenedBreaches);
        }

        [Test]
        public void WhenTheBiteChanceIsWeak_ThenABiteBreaksAModuleFarMoreRarelyThanAFullRockImpact() {
            float biteChance = _shipDamageConfiguration.ModuleBreakChance * _bugConfiguration.BiteStrength;
            float rockChance = _shipDamageConfiguration.ModuleBreakChance;
            RuleImpactDamageService justAboveBite = CreateImpacts(biteChance + ROLL_MARGIN);

            new BugGnawService(_hull, justAboveBite, _bugConfiguration).ServerBite(BUG_ID, _engine);

            Assert.IsEmpty(justAboveBite.BrokenSockets, "A bite broke a module with a roll above its chance.");
            Assert.Less(biteChance, rockChance * MAX_BITE_SHARE_OF_ROCK);
        }

        private RuleImpactDamageService CreateImpacts(float roll) {
            List<ShipDamageCandidate> modules = new() { new ShipDamageCandidate(ENGINE_SOCKET, _engine + Vector3.up, true, 1f) };
            List<ShipDamageCandidate> points = new() { new ShipDamageCandidate(HULL_POINT, _engine + Vector3.right, true, 1f) };
            return new RuleImpactDamageService(new ShipDamageRule(_shipDamageConfiguration), modules, points, roll);
        }
    }
}
