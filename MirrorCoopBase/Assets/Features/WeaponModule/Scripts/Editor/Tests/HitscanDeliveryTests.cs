using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using NUnit.Framework;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class HitscanDeliveryTests {
        private const float SPREAD_DEGREES = 0.6f;
        private const float RANGE_METERS = 400f;
        private const float ANGLE_TOLERANCE_DEGREES = 0.001f;
        private const ulong TARGET_ID = 42;
        private const string WEAPON_ID = "test-weapon";
        private const int RANDOM_STEPS = 21;
        // The largest value IWeaponRandom may return (its range is [0, 1)).
        private const float TOP_UNIT_VALUE = 0.9999f;
        private const float EDGE_TOLERANCE_DEGREES = 0.01f;
        private const float RANGE_TOLERANCE_METERS = 0.01f;

        private static readonly WeaponRaycastHit _targetHit = new WeaponRaycastHit(new Vector3(0f, 0f, 50f), Vector3.back, hasTarget: true, TARGET_ID);

        [TestCase(0f, 0f, 1f)]
        [TestCase(0f, 1f, 0f)]
        [TestCase(0.3f, -0.9f, 0.3f)]
        public void WhenShotsUseEveryRandomValue_ThenNoneLeavesTheSpreadCone(float aimX, float aimY, float aimZ) {
            Vector3 aim = new Vector3(aimX, aimY, aimZ).normalized;
            FakeWeaponRaycast raycast = new FakeWeaponRaycast(isHitting: false, default);

            for (int angleStep = 0; angleStep < RANDOM_STEPS; angleStep++) {
                for (int azimuthStep = 0; azimuthStep < RANDOM_STEPS; azimuthStep++) {
                    HitscanDelivery delivery = CreateDelivery(SPREAD_DEGREES, raycast, new SequenceWeaponRandom(ToUnitValue(angleStep), ToUnitValue(azimuthStep)));
                    delivery.Deliver(CreateShot(aim), new RecordingShotOutcomeReceiver());
                }
            }

            foreach (Vector3 direction in raycast.Directions)
                Assert.That(Vector3.Angle(aim, direction), Is.LessThanOrEqualTo(SPREAD_DEGREES + ANGLE_TOLERANCE_DEGREES));
        }

        [Test]
        public void WhenRandomIsAtItsTop_ThenTheShotLeansToTheConeEdge() {
            FakeWeaponRaycast raycast = new FakeWeaponRaycast(isHitting: false, default);

            CreateDelivery(SPREAD_DEGREES, raycast, new SequenceWeaponRandom(TOP_UNIT_VALUE, 0.25f)).Deliver(CreateShot(Vector3.forward), new RecordingShotOutcomeReceiver());

            Assert.AreEqual(SPREAD_DEGREES, Vector3.Angle(Vector3.forward, raycast.Directions[0]), EDGE_TOLERANCE_DEGREES);
        }

        [Test]
        public void WhenSpreadIsZero_ThenTheShotFollowsTheAim() {
            FakeWeaponRaycast raycast = new FakeWeaponRaycast(isHitting: false, default);

            CreateDelivery(0f, raycast, new SequenceWeaponRandom(TOP_UNIT_VALUE, 0.5f)).Deliver(CreateShot(Vector3.forward), new RecordingShotOutcomeReceiver());

            Assert.That(Vector3.Angle(Vector3.forward, raycast.Directions[0]), Is.LessThan(ANGLE_TOLERANCE_DEGREES));
        }

        [Test]
        public void WhenRayHitsATarget_ThenTheOutcomeCarriesTheTarget() {
            RecordingShotOutcomeReceiver receiver = new RecordingShotOutcomeReceiver();

            CreateDelivery(SPREAD_DEGREES, new FakeWeaponRaycast(isHitting: true, _targetHit), new SequenceWeaponRandom(0.5f)).Deliver(CreateShot(Vector3.forward), receiver);

            WeaponShotOutcome outcome = receiver.Outcomes[0];
            Assert.IsTrue(outcome.IsImpact);
            Assert.IsTrue(outcome.IsTargetHit);
            Assert.AreEqual(TARGET_ID, outcome.TargetId);
            Assert.AreEqual(_targetHit.Point, outcome.EndPoint);
        }

        [Test]
        public void WhenRayMisses_ThenTheOutcomeEndsAtTheRange() {
            RecordingShotOutcomeReceiver receiver = new RecordingShotOutcomeReceiver();

            CreateDelivery(0f, new FakeWeaponRaycast(isHitting: false, default), new SequenceWeaponRandom(0.5f)).Deliver(CreateShot(Vector3.forward), receiver);

            WeaponShotOutcome outcome = receiver.Outcomes[0];
            Assert.IsFalse(outcome.IsImpact);
            Assert.IsFalse(outcome.IsTargetHit);
            Assert.AreEqual(RANGE_METERS, outcome.EndPoint.magnitude, RANGE_TOLERANCE_METERS);
        }

        private static float ToUnitValue(int step) =>
            Mathf.Min(step / (float)(RANDOM_STEPS - 1), TOP_UNIT_VALUE);

        private static WeaponShot CreateShot(Vector3 aim) =>
            new WeaponShot(WEAPON_ID, new PlayerKey("client:test"), Vector3.zero, aim, RANGE_METERS);

        private static HitscanDelivery CreateDelivery(float spreadDegrees, IWeaponRaycast raycast, IWeaponRandom random) =>
            new HitscanDelivery(spreadDegrees, raycast, random);
    }
}
