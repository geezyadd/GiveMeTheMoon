using Features.SonarModule.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarScopeTests {
        private const float RANGE = 400f;
        private const float TOLERANCE = 0.001f;

        private SonarTestConfiguration _configuration;
        private SonarScopeLayout _layout;
        private SonarPingRule _pingRule;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _layout = new SonarScopeLayout();
            _pingRule = new SonarPingRule(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenAContactLiesAheadOfATurnedShip_ThenItIsStraightUpOnTheScope() {
            Vector3 ship = new Vector3(10f, 100f, 10f);
            Vector3 ahead = ship + Quaternion.Euler(0f, 90f, 0f) * Vector3.forward * (RANGE * 0.5f);

            Assert.IsTrue(_layout.TryPlace(ship, 90f, ahead, RANGE, false, out Vector2 scope));
            Assert.AreEqual(0f, scope.x, TOLERANCE);
            Assert.AreEqual(0.5f, scope.y, TOLERANCE);
        }

        [Test]
        public void WhenAContactLiesToTheRight_ThenItIsRightOnTheScope() {
            Assert.IsTrue(_layout.TryPlace(Vector3.zero, 0f, new Vector3(RANGE * 0.25f, 0f, 0f), RANGE, false, out Vector2 scope));
            Assert.AreEqual(0.25f, scope.x, TOLERANCE);
            Assert.AreEqual(0f, scope.y, TOLERANCE);
        }

        [Test]
        public void WhenAContactLiesBeyondRange_ThenOnlyAKindShownBeyondRangeSitsOnTheEdge() {
            Vector3 far = new Vector3(0f, 0f, -RANGE * 3f);

            Assert.IsFalse(_layout.TryPlace(Vector3.zero, 0f, far, RANGE, false, out _));
            Assert.IsTrue(_layout.TryPlace(Vector3.zero, 0f, far, RANGE, true, out Vector2 scope));
            Assert.AreEqual(-1f, scope.y, TOLERANCE);
        }

        [Test]
        public void WhenAPingWasJustSent_ThenTheNextWaitsForTheCooldown() {
            Assert.IsFalse(_pingRule.CanPing(SonarTestConfiguration.COOLDOWN_SECONDS - 0.1f, 0f));
            Assert.IsTrue(_pingRule.CanPing(SonarTestConfiguration.COOLDOWN_SECONDS, 0f));
            Assert.IsTrue(_pingRule.CanPing(0f, float.NegativeInfinity));
        }

        [Test]
        public void WhenTheWaveRuns_ThenItReachesTheEdgeAndTheEchoFades() {
            Assert.AreEqual(0.5f, _pingRule.GetWaveShare(SonarTestConfiguration.WAVE_SECONDS * 0.5f, 0f), TOLERANCE);
            Assert.AreEqual(1f, _pingRule.GetEchoAlpha(0f, 0f), TOLERANCE);
            Assert.AreEqual(0f, _pingRule.GetEchoAlpha(SonarTestConfiguration.ECHO_SECONDS, 0f), TOLERANCE);
        }
    }
}
