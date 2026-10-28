using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // The rope a harpoon shows: its tip flies out at the out speed, a rope that caught a target stays there for the
    // hold time, one that caught nothing is reeled in at once, and a reeled-in rope is gone.
    public sealed class RopeCastTests {
        private const float OUT_SPEED = 100f;
        private const float RETURN_SPEED = 50f;
        private const float HOLD_SECONDS = 0.3f;
        private const float LENGTH = 40f;
        private const float FRAME = 0.1f;

        private WeaponFeelProfile _profile;

        [SetUp]
        public void SetUp() {
            _profile = ScriptableObject.CreateInstance<WeaponFeelProfile>();
            SerializedObject profile = new SerializedObject(_profile);
            profile.FindProperty("_ropeOutSpeedMetersPerSecond").floatValue = OUT_SPEED;
            profile.FindProperty("_ropeReturnSpeedMetersPerSecond").floatValue = RETURN_SPEED;
            profile.FindProperty("_ropeHoldSeconds").floatValue = HOLD_SECONDS;
            profile.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_profile);

        [Test]
        public void WhenRopeIsThrown_ThenItsTipFliesOutAtTheOutSpeed() {
            RopeCast rope = new(_profile);
            rope.Throw(Vector3.zero, Vector3.forward * LENGTH, false);

            rope.Advance(FRAME);

            Assert.AreEqual(OUT_SPEED * FRAME, rope.GetTip(Vector3.zero).z, 0.001f);
            Assert.AreEqual(RopeCastPhase.Out, rope.Phase);
        }

        [Test]
        public void WhenRopeCatchesATarget_ThenItHoldsThereBeforeItIsReeledIn() {
            RopeCast rope = new(_profile);
            rope.Throw(Vector3.zero, Vector3.forward * LENGTH, true);

            Advance(rope, LENGTH / OUT_SPEED + FRAME);
            Assert.AreEqual(RopeCastPhase.Hold, rope.Phase);
            Assert.AreEqual(LENGTH, rope.GetTip(Vector3.zero).z, 0.001f, "A caught rope does not reach its target.");

            Advance(rope, HOLD_SECONDS + FRAME);
            Assert.AreEqual(RopeCastPhase.Return, rope.Phase);
        }

        [Test]
        public void WhenRopeCatchesNothing_ThenItIsReeledInAndGone() {
            RopeCast rope = new(_profile);
            rope.Throw(Vector3.zero, Vector3.forward * LENGTH, false);

            Advance(rope, LENGTH / OUT_SPEED + FRAME);
            Assert.AreEqual(RopeCastPhase.Return, rope.Phase, "A missed rope is not reeled in at once.");

            Advance(rope, LENGTH / RETURN_SPEED + FRAME);
            Assert.IsFalse(rope.IsVisible, "A reeled-in rope is still shown.");
        }

        private static void Advance(RopeCast rope, float seconds) {
            for (float elapsed = 0f; elapsed < seconds; elapsed += FRAME)
                rope.Advance(FRAME);
        }
    }
}
