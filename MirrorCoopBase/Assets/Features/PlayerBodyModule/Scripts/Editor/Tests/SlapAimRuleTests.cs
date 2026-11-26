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
    }
}
