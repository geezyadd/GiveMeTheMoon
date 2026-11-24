using Features.PlayerReplicaModule.Visual;
using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerReplicaModule.Editor.Tests {
    public sealed class VisualLagOffsetTests {
        private const float THRESHOLD = 0.05f;
        private const float MAX_DISTANCE = 10f;
        private const float CATCH_UP = 0.1f;
        private const float TOLERANCE = 0.0001f;

        private VisualLagOffset _offset;

        [SetUp]
        public void SetUp() =>
            _offset = new VisualLagOffset();

        [Test]
        public void WhenTheBodyJumps_ThenTheVisualStaysWhereItWas() {
            _offset.HideJump(new Vector3(1f, 0f, 0f), THRESHOLD, MAX_DISTANCE);

            AssertNear(new Vector3(-1f, 0f, 0f), _offset.Offset);
        }

        [Test]
        public void WhenTheJumpIsTiny_ThenItIsNotHidden() {
            _offset.HideJump(new Vector3(0.02f, 0f, 0f), THRESHOLD, MAX_DISTANCE);

            AssertNear(Vector3.zero, _offset.Offset);
        }

        [Test]
        public void WhenTheJumpIsHuge_ThenItIsShownAndAnyOldSlideStops() {
            _offset.HideJump(new Vector3(1f, 0f, 0f), THRESHOLD, MAX_DISTANCE);

            _offset.HideJump(new Vector3(40f, 0f, 0f), THRESHOLD, MAX_DISTANCE);

            AssertNear(Vector3.zero, _offset.Offset);
        }

        [Test]
        public void WhenTheCatchUpTimePasses_ThenAlmostNothingIsLeft() {
            _offset.HideJump(new Vector3(0f, 0f, 2f), THRESHOLD, MAX_DISTANCE);

            for (int frame = 0; frame < 10; frame++)
                _offset.Tick(CATCH_UP / 10f, CATCH_UP);

            Assert.AreEqual(0.1f, _offset.Offset.magnitude, 0.001f);
        }

        [Test]
        public void WhenTheSlideRunsOn_ThenItEndsAtZero() {
            _offset.HideJump(new Vector3(0f, 0f, 2f), THRESHOLD, MAX_DISTANCE);

            for (int frame = 0; frame < 100; frame++)
                _offset.Tick(0.02f, CATCH_UP);

            Assert.AreEqual(Vector3.zero, _offset.Offset);
        }

        [Test]
        public void WhenTwoJumpsComeOneAfterTheOther_ThenTheyAddUp() {
            _offset.HideJump(new Vector3(1f, 0f, 0f), THRESHOLD, MAX_DISTANCE);
            _offset.HideJump(new Vector3(0f, 0f, 1f), THRESHOLD, MAX_DISTANCE);

            AssertNear(new Vector3(-1f, 0f, -1f), _offset.Offset);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), TOLERANCE, $"expected {expected}, got {actual}");
    }
}
