using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts.Editor.Tests {
    public sealed class ShipHullSweepTests {
        private const float ROCK_RADIUS = 2f;
        private const float FAST_STEP_METERS = 30f;

        private static readonly ShipHullBox _box = new() { Center = Vector3.zero, HalfExtents = new Vector3(5f, 1.5f, 9f) };

        [Test]
        public void WhenAFastShipPassesARockBetweenTwoChecks_ThenTheSweptHullStillTouchesIt() {
            Pose from = new(Vector3.zero, Quaternion.identity);
            Pose to = new(Vector3.forward * FAST_STEP_METERS, Quaternion.identity);
            Vector3 rockBetween = Vector3.forward * (FAST_STEP_METERS * 0.5f);

            Assert.IsFalse(ShipHullSweep.Create(_box, to, to).Touches(rockBetween, ROCK_RADIUS), "The test rock must lie outside both poses.");
            Assert.IsFalse(ShipHullSweep.Create(_box, from, from).Touches(rockBetween, ROCK_RADIUS), "The test rock must lie outside both poses.");
            Assert.IsTrue(ShipHullSweep.Create(_box, from, to).Touches(rockBetween, ROCK_RADIUS), "The sweep skipped the rock.");
        }

        [Test]
        public void WhenTheHullIsTurned_ThenTheBoxTurnsWithIt() {
            Pose turned = new(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            Vector3 besideTheTurnedNose = Vector3.right * (_box.HalfExtents.z + ROCK_RADIUS * 0.5f);

            Assert.IsTrue(ShipHullSweep.Create(_box, turned, turned).Touches(besideTheTurnedNose, ROCK_RADIUS));
        }
    }
}
