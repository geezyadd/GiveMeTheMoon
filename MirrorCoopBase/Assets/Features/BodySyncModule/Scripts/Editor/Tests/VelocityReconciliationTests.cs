using Features.BodySyncModule.Correction;
using NUnit.Framework;
using UnityEngine;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class VelocityReconciliationTests {
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void WhenTheOwnerIsSlower_ThenTheCopyIsScaledDownKeepingItsDirection() {
            Vector3 result = VelocityReconciliation.Reconcile(new Vector3(4f, 0f, 0f), new Vector3(1f, 0f, 0f));

            Assert.AreEqual(1f, result.x, TOLERANCE);
        }

        [Test]
        public void WhenTheOwnerIsFaster_ThenOnlyTheMissingSpeedIsAdded() {
            Vector3 result = VelocityReconciliation.Reconcile(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 5f));

            Assert.AreEqual(5f, result.z, TOLERANCE);
        }

        [Test]
        public void WhenTheOwnerMovesTheOtherWayAndFaster_ThenTheMissingSpeedIsAddedInTheOwnersDirection() {
            Vector3 result = VelocityReconciliation.Reconcile(new Vector3(1f, 0f, 0f), new Vector3(-3f, 0f, 0f));

            Assert.AreEqual(-1f, result.x, TOLERANCE);
        }

        [Test]
        public void WhenTheOwnerStands_ThenTheCopyStops() {
            Vector3 result = VelocityReconciliation.Reconcile(new Vector3(3f, -2f, 1f), Vector3.zero);

            Assert.AreEqual(0f, result.magnitude, TOLERANCE);
        }

        [Test]
        public void WhenTheCopyStands_ThenItGetsTheOwnersVelocity() {
            var owner = new Vector3(2f, 1f, -3f);

            Vector3 result = VelocityReconciliation.Reconcile(Vector3.zero, owner);

            Assert.AreEqual(0f, Vector3.Distance(owner, result), TOLERANCE);
        }

        [Test]
        public void WhenTheAxesDiffer_ThenEachAxisFollowsItsOwnRule() {
            Vector3 result = VelocityReconciliation.Reconcile(new Vector3(4f, 0f, 1f), new Vector3(2f, 0f, 3f));

            Assert.AreEqual(2f, result.x, TOLERANCE);
            Assert.AreEqual(3f, result.z, TOLERANCE);
        }
    }
}
