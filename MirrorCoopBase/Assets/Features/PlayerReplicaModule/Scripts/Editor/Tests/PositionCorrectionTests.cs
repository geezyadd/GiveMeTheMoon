using Features.PlayerReplicaModule.Correction;
using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class PositionCorrectionTests {
        private const float TOLERANCE = 0.0001f;

        private static readonly BodyCorrectionSettings Settings = new() {
            PullShare = 0.1f,
            RestingTolerance = 0.15f,
            RestingPullShare = 0.02f,
            HeightToleranceStart = 0.3f,
            HeightToleranceFull = 0.6f,
            HeightStepLimit = 0.1f,
            LeadLimitSeconds = 0.15f,
            TeleportDistance = 3f,
            GroundMismatchSeconds = 0.5f
        };

        [Test]
        public void WhenTheErrorIsHorizontal_ThenTheStepRemovesTheSetShare() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(1f, 0f, -2f), true, false, Settings);

            Assert.IsFalse(step.IsSnap);
            AssertNear(new Vector3(0.1f, 0f, -0.2f), step.Offset);
        }

        [Test]
        public void WhenTheVerticalErrorIsInsideTheHeightTolerance_ThenTheHeightIsNotTouched() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(0f, 0.25f, 0f), true, false, Settings);

            Assert.AreEqual(0f, step.Offset.y, TOLERANCE);
        }

        [Test]
        public void WhenTheVerticalErrorIsHalfwayAcrossTheHeightTolerance_ThenHalfThePullWorks() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(0f, -0.45f, 0f), true, false, Settings);

            Assert.AreEqual(-0.05f, step.Offset.y, TOLERANCE);
        }

        [Test]
        public void WhenTheVerticalErrorIsPastTheHeightTolerance_ThenTheMoveIsCappedPerStep() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(0f, 2f, 0f), true, false, Settings);

            Assert.AreEqual(0.1f, step.Offset.y, TOLERANCE);
        }

        [Test]
        public void WhenTheCopyIsStuck_ThenASmallVerticalErrorIsClosedWithoutOvershoot() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(0f, 0.04f, 0f), true, true, Settings);

            Assert.AreEqual(0.04f, step.Offset.y, TOLERANCE);
        }

        [Test]
        public void WhenTheErrorIsPastTheTeleportDistance_ThenTheCopySnapsByTheWholeError() {
            var error = new Vector3(2.5f, 0f, 2.5f);

            PositionCorrectionStep step = PositionCorrection.Compute(error, true, false, Settings);

            Assert.IsTrue(step.IsSnap);
            AssertNear(error, step.Offset);
        }

        [Test]
        public void WhenThereIsNoError_ThenThereIsNoMove() {
            PositionCorrectionStep step = PositionCorrection.Compute(Vector3.zero, true, true, Settings);

            Assert.IsFalse(step.IsSnap);
            AssertNear(Vector3.zero, step.Offset);
        }

        [Test]
        public void WhenTheOwnerStopsAndTheCopyIsJustPast_ThenItIsNotPulled() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(-0.1f, 0f, 0.05f), false, false, Settings);

            AssertNear(Vector3.zero, step.Offset);
        }

        [Test]
        public void WhenTheOwnerStopsAndTheCopyIsFarPast_ThenItIsPulledGently() {
            PositionCorrectionStep step = PositionCorrection.Compute(new Vector3(-0.5f, 0f, 0f), false, false, Settings);

            Assert.IsFalse(step.IsSnap);
            AssertNear(new Vector3(-0.01f, 0f, 0f), step.Offset);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), TOLERANCE, $"expected {expected}, got {actual}");
    }
}
