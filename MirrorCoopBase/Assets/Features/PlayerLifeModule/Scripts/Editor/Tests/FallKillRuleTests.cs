using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerLifeModule.Scripts.Editor.Tests {
    public sealed class FallKillRuleTests {
        private PlayerDamageConfiguration _configuration;
        private FallKillRule _rule;

        [SetUp]
        public void SetUp() {
            // The default asset values: 30 m below the pad, 30 m below the deck.
            _configuration = ScriptableObject.CreateInstance<PlayerDamageConfiguration>();
            _rule = new FallKillRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenAboveKillDepth_ThenNotBelow() {
            Assert.That(_rule.IsBelowKillHeight(-29f, 0f, false), Is.False);
        }

        [Test]
        public void WhenBelowKillDepthAtStation_ThenBelow() {
            Assert.That(_rule.IsBelowKillHeight(-31f, 0f, false), Is.True);
        }

        [Test]
        public void WhenBelowDeckInFlight_ThenBelow() {
            Assert.That(_rule.IsBelowKillHeight(69f, 100f, true), Is.True);
        }
    }
}
