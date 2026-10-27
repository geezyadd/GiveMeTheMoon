using Features.HandWeaponModule.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace Features.HandWeaponModule.Editor.Tests {
    public sealed class HandWeaponPoseTests {
        private const float TOLERANCE = 1e-4f;

        private static readonly Vector3 _viewOffset = new Vector3(0.17f, -0.16f, 0.42f);
        private static readonly Vector3 _handOffset = new Vector3(0.55f, -0.05f, -0.15f);

        private readonly HandWeaponPose _pose = new HandWeaponPose();

        private Transform _arm;
        private Transform _view;

        [SetUp]
        public void SetUp() {
            _arm = new GameObject("Arm").transform;
            _view = new GameObject("View").transform;
            _arm.SetPositionAndRotation(new Vector3(1f, 1.2f, 0.7f), Quaternion.Euler(0f, 30f, 0f));
            _view.SetPositionAndRotation(new Vector3(0.8f, 1.65f, 0f), Quaternion.Euler(20f, 30f, 0f));
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_arm.gameObject);
            Object.DestroyImmediate(_view.gameObject);
        }

        [Test]
        public void WhenPlacedInView_ThenTheWeaponSitsAtTheOffsetInTheViewAndLooksAlongIt() {
            Pose local = _pose.InView(_arm, _view, _viewOffset);

            Vector3 worldPosition = _arm.TransformPoint(local.position);
            Quaternion worldRotation = _arm.rotation * local.rotation;
            Assert.Less(Vector3.Distance(_view.TransformPoint(_viewOffset), worldPosition), TOLERANCE);
            Assert.Less(Quaternion.Angle(_view.rotation, worldRotation), TOLERANCE * 10f);
        }

        [Test]
        public void WhenTheViewPitches_ThenTheWeaponInViewPitchesWithIt() {
            Pose level = _pose.InView(_arm, _view, _viewOffset);
            _view.rotation = Quaternion.Euler(-30f, 30f, 0f);

            Pose pitched = _pose.InView(_arm, _view, _viewOffset);

            Assert.AreEqual(50f, Quaternion.Angle(level.rotation, pitched.rotation), TOLERANCE * 100f);
        }

        [Test]
        public void WhenPlacedInHand_ThenTheWeaponSitsAtTheHandOffsetAndPointsAlongTheAim() {
            Vector3 aim = Quaternion.Euler(-10f, 45f, 0f) * Vector3.forward;

            Pose local = _pose.InHand(_arm, aim, _handOffset);

            Assert.AreEqual(_handOffset, local.position);
            Assert.Less(Vector3.Angle(aim, _arm.rotation * local.rotation * Vector3.forward), TOLERANCE * 10f);
        }

        [Test]
        public void WhenTheAimIsEmpty_ThenTheWeaponInHandPointsAlongTheArm() {
            Pose local = _pose.InHand(_arm, Vector3.zero, _handOffset);

            Assert.AreEqual(Quaternion.identity, local.rotation);
        }
    }
}
