using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerBodyModule.Editor.Tests {
    public sealed class HitPushRuleTests {
        [Test]
        public void WhenTheLimbIsFast_ThenTheHeadGetsItsVelocityScaled() {
            Vector3 velocity = HitPushRule.HeadVelocity(new Vector3(4f, 0f, 0f), Vector3.forward, 2.5f, 6f);

            Assert.AreEqual(10f, velocity.x, 0.001f);
            Assert.AreEqual(0f, velocity.z, 0.001f);
        }

        [Test]
        public void WhenTheLimbIsSlow_ThenTheHeadGetsTheMinimumAlongTheLimbsMotion() {
            Vector3 velocity = HitPushRule.HeadVelocity(new Vector3(0f, 0f, 1f), Vector3.right, 2.5f, 6f);

            Assert.AreEqual(6f, velocity.z, 0.001f);
            Assert.AreEqual(0f, velocity.x, 0.001f);
        }

        [Test]
        public void WhenTheLimbStands_ThenTheHeadGetsTheMinimumFromTheLimbTowardIt() {
            Vector3 velocity = HitPushRule.HeadVelocity(Vector3.zero, new Vector3(0.2f, 0f, 0f), 2.5f, 6f);

            Assert.AreEqual(6f, velocity.x, 0.001f);
        }
    }
}
