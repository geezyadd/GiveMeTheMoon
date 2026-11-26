using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerBodyModule.Editor.Tests {
    public sealed class SlapAimRuleTests {
        private const float MaxDistance = 1.3f;
        private const float MaxAngle = 60f;

        [Test]
        public void WhenNoHeadIsNear_ThenNoneIsPicked() {
            int picked = SlapAimRule.PickHead(Vector3.zero, 0f, new[] { new Vector3(0f, 0f, 3f) }, MaxDistance, MaxAngle);

            Assert.AreEqual(-1, picked);
        }

        [Test]
        public void WhenTheHeadIsBehind_ThenItIsNotPicked() {
            int picked = SlapAimRule.PickHead(Vector3.zero, 0f, new[] { new Vector3(0f, 0f, -1f) }, MaxDistance, MaxAngle);

            Assert.AreEqual(-1, picked);
        }

        [Test]
        public void WhenTwoHeadsAreInFront_ThenTheNearerIsPicked() {
            Vector3[] heads = { new(0f, 0f, 1.2f), new(0.3f, 0.1f, 0.8f) };

            int picked = SlapAimRule.PickHead(Vector3.zero, 0f, heads, MaxDistance, MaxAngle);

            Assert.AreEqual(1, picked);
        }

        [Test]
        public void WhenTheSlapperFacesSideways_ThenTheHeadOnThatSideIsPicked() {
            Vector3[] heads = { new(0f, 0f, 1f), new(1f, 0f, 0f) };

            int picked = SlapAimRule.PickHead(Vector3.zero, 90f, heads, MaxDistance, MaxAngle);

            Assert.AreEqual(1, picked);
        }

        [Test]
        public void WhenTheHeadIsRightBelow_ThenTheAngleDoesNotRuleItOut() {
            int picked = SlapAimRule.PickHead(Vector3.zero, 0f, new[] { new Vector3(0f, -0.5f, 0f) }, MaxDistance, MaxAngle);

            Assert.AreEqual(0, picked);
        }

        [TestCase(-0.1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.1f, 0f)]
        [TestCase(0.2f, 0f)]
        [TestCase(0.27f, 0.5f)]
        [TestCase(0.34f, 1f)]
        [TestCase(0.45f, 1f)]
        [TestCase(0.65f, 0.5f)]
        [TestCase(0.8f, 0f)]
        [TestCase(1f, 0f)]
        public void TheReachWeight_WaitsForTheWindUpThenEasesInHoldsAndEasesOut(float sinceSlap, float expected) {
            Assert.AreEqual(expected, SlapAimRule.Weight(sinceSlap, 0.2f, 0.34f, 0.5f, 0.8f), 0.001f);
        }
    }
}
