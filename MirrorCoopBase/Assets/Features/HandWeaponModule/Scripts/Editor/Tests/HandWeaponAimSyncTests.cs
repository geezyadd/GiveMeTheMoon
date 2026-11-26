using Features.HandWeaponModule.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace Features.HandWeaponModule.Editor.Tests {
    // The holder's aim goes out as the view turns, not more often than its interval, and other peers turn toward it
    // smoothly.
    public sealed class HandWeaponAimSyncTests {
        private const float INTERVAL_SECONDS = 0.05f;
        private const float MIN_DEGREES = 1f;
        private const float SMOOTHING_SECONDS = 0.08f;

        [Test]
        public void WhenTheAimTurnsFarEnough_ThenItIsSentButNotAgainWithinTheInterval() {
            HandWeaponAimSync sync = new HandWeaponAimSync(INTERVAL_SECONDS, MIN_DEGREES, SMOOTHING_SECONDS);
            Vector3 turned = Quaternion.Euler(0f, 10f, 0f) * Vector3.forward;

            Assert.IsTrue(sync.IsSendDue(turned, 0d));
            sync.MarkSent(turned, 0d);
            Vector3 turnedMore = Quaternion.Euler(0f, 20f, 0f) * Vector3.forward;
            Assert.IsFalse(sync.IsSendDue(turnedMore, 0.01d), "Sent again within the interval.");
            Assert.IsTrue(sync.IsSendDue(turnedMore, 0.06d), "Not sent after the interval.");
        }

        [Test]
        public void WhenTheAimBarelyMoves_ThenNothingIsSent() {
            HandWeaponAimSync sync = new HandWeaponAimSync(INTERVAL_SECONDS, MIN_DEGREES, SMOOTHING_SECONDS);
            sync.MarkSent(Vector3.forward, 0d);

            Assert.IsFalse(sync.IsSendDue(Quaternion.Euler(0f, 0.5f, 0f) * Vector3.forward, 1d));
        }

        [Test]
        public void WhenANewAimArrives_ThenTheShownAimTurnsTowardItWithoutSnapping() {
            HandWeaponAimSync sync = new HandWeaponAimSync(INTERVAL_SECONDS, MIN_DEGREES, SMOOTHING_SECONDS);
            Vector3 received = Vector3.right;

            Vector3 shown = sync.Smooth(Vector3.forward, received, 0.016f);
            Assert.Greater(Vector3.Angle(shown, received), 1f, "The shown aim snapped to the received one.");
            for (int i = 0; i < 60; i++)
                shown = sync.Smooth(shown, received, 0.016f);

            Assert.Less(Vector3.Angle(shown, received), 1f, "The shown aim did not reach the received one in a second.");
        }
    }
}
