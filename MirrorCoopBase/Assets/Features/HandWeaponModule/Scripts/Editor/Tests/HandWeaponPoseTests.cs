using Features.HandWeaponModule.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace Features.HandWeaponModule.Editor.Tests {
    // The weapon in the hand: its grip lands in the palm and its barrel points along the aim, wherever the arm point is.
    public sealed class HandWeaponPoseTests {
        private const float TOLERANCE = 1e-4f;

        private static readonly Vector3 _gripOffset = new Vector3(0f, -0.06f, -0.03f);
        private static readonly Vector3 _palm = new Vector3(1.4f, 1.1f, 1.2f);

        private readonly HandWeaponPose _pose = new HandWeaponPose();

        private Transform _arm;

        [SetUp]
        public void SetUp() {
            _arm = new GameObject("Arm").transform;
            _arm.SetPositionAndRotation(new Vector3(1f, 1.2f, 0.7f), Quaternion.Euler(0f, 30f, 0f));
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_arm.gameObject);

        [Test]
        public void WhenPlacedAtTheGrip_ThenTheGripPointLandsInThePalm() {
            Vector3 aim = Quaternion.Euler(-10f, 45f, 0f) * Vector3.forward;

            Pose local = _pose.AtGrip(_arm, _palm, aim, _gripOffset);

            Vector3 worldPosition = _arm.TransformPoint(local.position);
            Quaternion worldRotation = _arm.rotation * local.rotation;
            Assert.Less(Vector3.Distance(_palm, worldPosition + worldRotation * _gripOffset), TOLERANCE);
        }

        [Test]
        public void WhenPlacedAtTheGrip_ThenTheBarrelPointsAlongTheAimAndStaysUpright() {
            Vector3 aim = Quaternion.Euler(-10f, 45f, 0f) * Vector3.forward;

            Pose local = _pose.AtGrip(_arm, _palm, aim, _gripOffset);

            Quaternion worldRotation = _arm.rotation * local.rotation;
            Assert.Less(Vector3.Angle(aim, worldRotation * Vector3.forward), TOLERANCE * 10f);
            Assert.Less(Mathf.Abs((worldRotation * Vector3.right).y), TOLERANCE);
        }

        [Test]
        public void WhenTheAimIsEmpty_ThenTheWeaponPointsAlongTheArm() {
            Pose local = _pose.AtGrip(_arm, _palm, Vector3.zero, _gripOffset);

            Assert.Less(Quaternion.Angle(Quaternion.identity, local.rotation), TOLERANCE * 10f);
        }
    }
}
