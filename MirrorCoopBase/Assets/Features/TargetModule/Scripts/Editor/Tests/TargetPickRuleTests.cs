using NUnit.Framework;
using UnityEngine;

namespace Features.TargetModule.Editor.Tests {
    public sealed class TargetPickRuleTests {
        private const float MaxDistance = 1.3f;
        private const float MaxAngle = 60f;

        [Test]
        public void WhenNoPointIsNear_ThenNoneIsPicked() {
            int picked = Pick(Vector3.forward, new Vector3(0f, 0f, 3f));

            Assert.AreEqual(-1, picked);
        }

        [Test]
        public void WhenThePointIsBehind_ThenItIsNotPicked() {
            int picked = Pick(Vector3.forward, new Vector3(0f, 0f, -1f));

            Assert.AreEqual(-1, picked);
        }

        [Test]
        public void WhenTwoPointsAreInFront_ThenTheNearerIsPicked() {
            int picked = Pick(Vector3.forward, new Vector3(0f, 0f, 1.2f), new Vector3(0.3f, 0.1f, 0.8f));

            Assert.AreEqual(1, picked);
        }

        [Test]
        public void WhenTheSeekerFacesSideways_ThenThePointOnThatSideIsPicked() {
            int picked = Pick(Vector3.right, new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f));

            Assert.AreEqual(1, picked);
        }

        [Test]
        public void WhenThePointIsRightBelow_ThenTheAngleDoesNotRuleItOut() {
            int picked = Pick(Vector3.forward, new Vector3(0f, -0.5f, 0f));

            Assert.AreEqual(0, picked);
        }

        [Test]
        public void WhenTheForwardTiltsUp_ThenOnlyItsGroundDirectionCounts() {
            int picked = Pick(new Vector3(0f, 0.9f, 0.1f), new Vector3(0f, 0f, 1f));

            Assert.AreEqual(0, picked);
        }

        private static int Pick(Vector3 forward, params Vector3[] points) =>
            TargetPickRule.PickNearest(Vector3.zero, forward, points, MaxDistance, MaxAngle);
    }
}
