using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Features.WeaponModule.Scripts.Shooting;
using NUnit.Framework;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // The fire mode births the shots, the delivery says where each one lands, and a launch only leaves the muzzle.
    public sealed class WeaponShooterTests {
        private const float SHOTS_PER_SECOND = 20f;
        private const ulong TARGET_ID = 9UL;
        private const int FRAMES = 60;
        private const float FRAME_SECONDS = 1f / FRAMES;
        private const float MIDDLE_OF_RANGE = 0.5f;

        private WeaponDefinition _weapon;
        private RecordingWeaponFeel _feel;

        [SetUp]
        public void SetUp() {
            _weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            _feel = new RecordingWeaponFeel();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_weapon);

        [Test]
        public void WhenTriggerIsHeldForOneSecond_ThenTheFireModesShotsAreBornAndTheFeelShowsFiring() {
            WeaponShooter shooter = CreateShooter(new HitscanDelivery(0f, new TargetEverywhereRaycast(TARGET_ID), new SequenceWeaponRandom(MIDDLE_OF_RANGE)));

            int shots = 0;
            for (int frame = 0; frame < FRAMES; frame++)
                shots += shooter.Advance(true, FRAME_SECONDS, _feel);

            Assert.AreEqual(SHOTS_PER_SECOND, shots, 1f);
            Assert.IsTrue(_feel.IsFiring);
        }

        [Test]
        public void WhenARayShotIsFired_ThenItsOutcomeIsTheStruckTarget() {
            WeaponShooter shooter = CreateShooter(new HitscanDelivery(0f, new TargetEverywhereRaycast(TARGET_ID), new SequenceWeaponRandom(MIDDLE_OF_RANGE)));

            WeaponShotOutcome outcome = shooter.Fire(Vector3.zero, Vector3.forward, 0);

            Assert.IsTrue(outcome.IsTargetHit);
            Assert.AreEqual(TARGET_ID, outcome.TargetId);
        }

        [Test]
        public void WhenALaunchIsFired_ThenItStrikesNothingAndLeavesAlongTheAim() {
            WeaponShooter shooter = CreateShooter(new LaunchDelivery(0f, new SequenceWeaponRandom(MIDDLE_OF_RANGE)));

            WeaponShotOutcome outcome = shooter.Fire(Vector3.zero, Vector3.forward, 0);

            Assert.IsFalse(outcome.IsImpact);
            Assert.IsFalse(outcome.IsTargetHit);
            Assert.Greater(Vector3.Dot((outcome.EndPoint - outcome.Shot.Origin).normalized, Vector3.forward), 0.999f);
        }

        private WeaponShooter CreateShooter(IShotDelivery delivery) =>
            new WeaponShooter(_weapon, new AutoFireMode(0f, 0f, SHOTS_PER_SECOND), delivery);
    }
}
