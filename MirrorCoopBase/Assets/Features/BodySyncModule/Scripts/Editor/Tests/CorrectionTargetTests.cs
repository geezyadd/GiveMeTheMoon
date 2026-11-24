using Features.BodySyncModule.Correction;
using NUnit.Framework;
using UnityEngine;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class CorrectionTargetTests {
        private const float MAX_EXTRAPOLATION = 0.15f;
        private const float TOLERANCE = 0.0001f;

        private static readonly Vector3 Position = new(1f, 0f, 2f);
        private static readonly Vector3 Velocity = new(4f, 0f, 0f);

        [Test]
        public void WhenThePacketIsFresh_ThenTheTargetIsItsPosition() =>
            AssertNear(Position, CorrectionTarget.Predict(Position, Velocity, 0d, MAX_EXTRAPOLATION));

        [Test]
        public void WhenThePacketIsSomeMillisecondsOld_ThenTheTargetMovesAlongItsVelocity() =>
            AssertNear(new Vector3(1.4f, 0f, 2f), CorrectionTarget.Predict(Position, Velocity, 0.1d, MAX_EXTRAPOLATION));

        [Test]
        public void WhenThePacketIsVeryOld_ThenTheMoveIsCapped() =>
            AssertNear(new Vector3(1.6f, 0f, 2f), CorrectionTarget.Predict(Position, Velocity, 2d, MAX_EXTRAPOLATION));

        [Test]
        public void WhenTheAgeIsNegative_ThenItCountsAsZero() =>
            AssertNear(Position, CorrectionTarget.Predict(Position, Velocity, -0.05d, MAX_EXTRAPOLATION));

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), TOLERANCE, $"expected {expected}, got {actual}");
    }
}
