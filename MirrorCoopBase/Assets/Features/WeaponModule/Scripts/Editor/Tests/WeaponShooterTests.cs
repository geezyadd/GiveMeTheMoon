using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Features.WeaponModule.Scripts.Shooting;
using NUnit.Framework;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Every shot the fire mode births is shown; only the holding peer reports target hits.
    public sealed class WeaponShooterTests {
        private const float SHOTS_PER_SECOND = 20f;
        private const ulong TARGET_ID = 9UL;
        private const int FRAMES = 60;
        private const float FRAME_SECONDS = 1f / FRAMES;
        private const float MIDDLE_OF_RANGE = 0.5f;

        private WeaponDefinition _weapon;
        private RecordingWeaponFeel _feel;
        private RecordingHitReporter _reporter;
        private FixedWeaponHolder _holder;

        [SetUp]
        public void SetUp() {
            _weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            _feel = new RecordingWeaponFeel();
            _reporter = new RecordingHitReporter();
            _holder = new FixedWeaponHolder { IsTriggerHeld = true };
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_weapon);

        [Test]
        public void WhenOwnerHoldsTriggerForOneSecond_ThenEveryShotIsShownAndEveryTargetHitReported() {
            WeaponShooter shooter = CreateShooter(new ReportedShotOutcomes(_feel, _reporter));

            AdvanceOneSecond(shooter);

            Assert.AreEqual(SHOTS_PER_SECOND, _feel.Shots, 1f);
            Assert.AreEqual(_feel.Shots, _reporter.Reports);
            Assert.IsTrue(_feel.IsFiring);
        }

        [Test]
        public void WhenAnotherPeerReplaysTheFire_ThenShotsShowButNothingIsReported() {
            WeaponShooter shooter = CreateShooter(new ShownShotOutcomes(_feel));

            AdvanceOneSecond(shooter);

            Assert.Greater(_feel.Shots, 0);
            Assert.AreEqual(0, _reporter.Reports);
        }

        private void AdvanceOneSecond(WeaponShooter shooter) {
            for (int frame = 0; frame < FRAMES; frame++)
                shooter.Advance(_holder, FRAME_SECONDS);
        }

        private WeaponShooter CreateShooter(IShotOutcomeReceiver outcomes) =>
            new WeaponShooter(
                _weapon,
                new AutoFireMode(0f, 0f, SHOTS_PER_SECOND),
                new HitscanDelivery(0f, new TargetEverywhereRaycast(TARGET_ID), new SequenceWeaponRandom(MIDDLE_OF_RANGE)),
                _feel,
                outcomes);
    }
}
