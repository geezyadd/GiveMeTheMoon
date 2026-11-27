using Features.HandWeaponModule.Scripts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using NUnit.Framework;
using UnityEngine;

namespace Features.HandWeaponModule.Editor.Tests {
    // The weapon aims from the hand at the crosshair point, and along the view when that point is too close or behind.
    public sealed class CrosshairAimTests {
        private const float MIN_AIM_DISTANCE = 1.5f;
        private const float RANGE = 100f;

        private Transform _view;

        [SetUp]
        public void SetUp() {
            _view = new GameObject("View").transform;
            _view.SetPositionAndRotation(new Vector3(0f, 2f, 0f), Quaternion.identity);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_view.gameObject);

        [Test]
        public void WhenTheCrosshairPointIsFar_ThenTheWeaponAimsFromTheHandAtIt() {
            Vector3 point = new Vector3(0f, 2f, 20f);
            Vector3 hand = new Vector3(0.4f, 1.6f, 0.6f);

            Vector3 direction = new CrosshairAim(new PointRaycast(point), 0, MIN_AIM_DISTANCE).GetShotDirection(_view, hand, RANGE);

            Assert.Less(Vector3.Angle((point - hand).normalized, direction), 0.01f);
        }

        [Test]
        public void WhenTheCrosshairPointIsAtTheBarrel_ThenTheWeaponAimsAlongTheView() {
            Vector3 hand = new Vector3(0.4f, 1.6f, 0.6f);
            Vector3 point = hand + new Vector3(0f, -0.3f, 0.5f);

            Vector3 direction = new CrosshairAim(new PointRaycast(point), 0, MIN_AIM_DISTANCE).GetShotDirection(_view, hand, RANGE);

            Assert.AreEqual(_view.forward, direction);
        }

        [Test]
        public void WhenTheCrosshairPointIsBehindTheHand_ThenTheWeaponAimsAlongTheView() {
            Vector3 hand = new Vector3(0.4f, 1.6f, 3f);
            Vector3 point = new Vector3(0f, 0f, 1f);

            Vector3 direction = new CrosshairAim(new PointRaycast(point), 0, MIN_AIM_DISTANCE).GetShotDirection(_view, hand, RANGE);

            Assert.AreEqual(_view.forward, direction);
        }

        [Test]
        public void WhenTheViewLowersOntoTheFloor_ThenTheAimTurnsWithoutAJump() {
            Vector3 hand = new Vector3(0.4f, 1.4f, 0.6f);
            Vector3 previous = Vector3.zero;
            for (float pitch = 5f; pitch <= 85f; pitch += 0.5f) {
                _view.rotation = Quaternion.Euler(pitch, 0f, 0f);
                // The crosshair rests on the floor (y = 0) under the view.
                Vector3 floorPoint = _view.position + _view.forward * (_view.position.y / Mathf.Max(0.01f, -_view.forward.y));
                Vector3 direction = new CrosshairAim(new PointRaycast(floorPoint), 0, MIN_AIM_DISTANCE).GetShotDirection(_view, hand, RANGE);
                if (previous != Vector3.zero)
                    Assert.Less(Vector3.Angle(previous, direction), 3f, $"The aim jumped at a view pitch of {pitch}°.");

                previous = direction;
            }
        }

        private sealed class PointRaycast : IWeaponRaycast {
            private readonly Vector3 _point;

            public PointRaycast(Vector3 point) =>
                _point = point;

            public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, uint ignoredReceiverId, out WeaponRaycastHit hit) {
                hit = new WeaponRaycastHit(_point, -direction, hasTarget: false, targetId: 0);
                return true;
            }
        }
    }
}
