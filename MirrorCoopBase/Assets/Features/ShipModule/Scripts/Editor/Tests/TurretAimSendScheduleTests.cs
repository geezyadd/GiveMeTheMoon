using Features.ShipModule.Scripts.Turret;
using NUnit.Framework;

namespace Features.ShipModule.Editor.Tests {
    // The owner's aim goes out throttled; a trigger change never waits.
    public sealed class TurretAimSendScheduleTests {
        private const float INTERVAL_SECONDS = 0.05f;

        [Test]
        public void WhenNothingWasSentYet_ThenAimIsDue() =>
            Assert.IsTrue(new TurretAimSendSchedule(INTERVAL_SECONDS).IsDue(false, 0d));

        [Test]
        public void WhenAimWasJustSent_ThenNextAimWaitsForTheInterval() {
            TurretAimSendSchedule schedule = new TurretAimSendSchedule(INTERVAL_SECONDS);
            schedule.MarkSent(false, 1d);

            Assert.IsFalse(schedule.IsDue(false, 1.02d));
            Assert.IsTrue(schedule.IsDue(false, 1.06d));
        }

        [Test]
        public void WhenTriggerChanges_AndIntervalIsNotOver_ThenAimIsDueAtOnce() {
            TurretAimSendSchedule schedule = new TurretAimSendSchedule(INTERVAL_SECONDS);
            schedule.MarkSent(false, 1d);

            Assert.IsTrue(schedule.IsDue(true, 1.01d));
        }
    }
}
