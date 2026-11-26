using NUnit.Framework;
using UnityEngine;

namespace Features.AttackModule.Editor.Tests {
    public sealed class AttackHitRuleTests {
        [Test]
        public void WhenTheTargetIsAheadOfTheLimb_ThenItIsMeasuredFromTheReachNotTheLimb() {
            float distance = AttackHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0f, 0f, 1f), 0.4f);

            Assert.AreEqual(0.6f, distance, 0.001f);
        }

        [Test]
        public void WhenTheTargetIsBesideTheReach_ThenOnlyItsSideDistanceCounts() {
            float distance = AttackHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0.5f, 0f, 0.2f), 0.4f);

            Assert.AreEqual(0.5f, distance, 0.001f);
        }

        [Test]
        public void WhenTheTargetIsBehindTheLimb_ThenTheReachDoesNotHelp() {
            float distance = AttackHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0f, 0f, -0.7f), 0.4f);

            Assert.AreEqual(0.7f, distance, 0.001f);
        }

        [Test]
        public void WhenTheLimbStands_ThenTheDistanceIsFromTheLimb() {
            float distance = AttackHitRule.DistanceToReach(Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 0.8f), 0.4f);

            Assert.AreEqual(0.8f, distance, 0.001f);
        }
    }
}
