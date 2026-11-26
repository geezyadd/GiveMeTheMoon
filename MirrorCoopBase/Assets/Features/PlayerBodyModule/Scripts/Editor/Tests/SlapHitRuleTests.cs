using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerBodyModule.Editor.Tests {
    public sealed class SlapHitRuleTests {
        [Test]
        public void WhenTheHandIsFast_ThenTheHeadGetsItsVelocityScaled() {
            Vector3 velocity = SlapHitRule.HeadVelocity(new Vector3(4f, 0f, 0f), Vector3.forward, 2.5f, 6f);

            Assert.AreEqual(10f, velocity.x, 0.001f);
            Assert.AreEqual(0f, velocity.z, 0.001f);
        }

        [Test]
        public void WhenTheHandIsSlow_ThenTheHeadGetsTheMinimumAlongTheHandsMotion() {
            Vector3 velocity = SlapHitRule.HeadVelocity(new Vector3(0f, 0f, 1f), Vector3.right, 2.5f, 6f);

            Assert.AreEqual(6f, velocity.z, 0.001f);
            Assert.AreEqual(0f, velocity.x, 0.001f);
        }

        [Test]
        public void WhenTheHandStands_ThenTheHeadGetsTheMinimumFromTheHandTowardIt() {
            Vector3 velocity = SlapHitRule.HeadVelocity(Vector3.zero, new Vector3(0.2f, 0f, 0f), 2.5f, 6f);

            Assert.AreEqual(6f, velocity.x, 0.001f);
        }

        [Test]
        public void WhenTheHeadIsAheadOfTheHand_ThenItIsMeasuredFromTheReachNotTheHand() {
            float distance = SlapHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0f, 0f, 1f), 0.4f);

            Assert.AreEqual(0.6f, distance, 0.001f);
        }

        [Test]
        public void WhenTheHeadIsBesideTheReach_ThenOnlyItsSideDistanceCounts() {
            float distance = SlapHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0.5f, 0f, 0.2f), 0.4f);

            Assert.AreEqual(0.5f, distance, 0.001f);
        }

        [Test]
        public void WhenTheHeadIsBehindTheHand_ThenTheReachDoesNotHelp() {
            float distance = SlapHitRule.DistanceToReach(Vector3.zero, Vector3.forward * 5f, new Vector3(0f, 0f, -0.7f), 0.4f);

            Assert.AreEqual(0.7f, distance, 0.001f);
        }

        [Test]
        public void WhenTheHandStands_ThenTheDistanceIsFromTheHand() {
            float distance = SlapHitRule.DistanceToReach(Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 0.8f), 0.4f);

            Assert.AreEqual(0.8f, distance, 0.001f);
        }
    }
}
