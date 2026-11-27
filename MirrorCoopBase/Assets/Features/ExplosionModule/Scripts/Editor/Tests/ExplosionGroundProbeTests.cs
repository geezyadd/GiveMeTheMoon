using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Features.ExplosionModule.Editor.Tests {
    public sealed class ExplosionGroundProbeTests {
        private const float FLAT_RADIUS = 2f;
        private static readonly Vector3 _origin = new(5000f, 0f, 5000f);
        private static readonly ExplosionGroundSettings _settings = new(1 << 0, 1f, 30f, 0.6f);

        private readonly List<GameObject> _made = new();
        private ExplosionGroundProbe _probe;

        [SetUp]
        public void SetUp() =>
            _probe = new ExplosionGroundProbe();

        [TearDown]
        public void TearDown() {
            foreach (GameObject made in _made)
                Object.DestroyImmediate(made);
            _made.Clear();
        }

        [Test]
        public void WhenItGoesOffOnWideFlatGround_ThenItIsOnTheGround() {
            MakeFloor(Vector3.zero, 20f, 0f);

            Assert.IsTrue(_probe.TryFindGround(_origin + Vector3.up * 0.1f, FLAT_RADIUS, _settings, out Vector3 point));
            Assert.AreEqual(0f, point.y - _origin.y, 0.01f);
        }

        [Test]
        public void WhenItGoesOffHighAboveTheGround_ThenItIsInTheAir() {
            MakeFloor(Vector3.zero, 20f, 0f);

            Assert.IsFalse(_probe.TryFindGround(_origin + Vector3.up * 3f, FLAT_RADIUS, _settings, out _));
        }

        [Test]
        public void WhenItGoesOffNearAnEdge_ThenItIsInTheAir() {
            MakeFloor(Vector3.zero, 20f, 0f);
            Vector3 nearEdge = _origin + new Vector3(10f - FLAT_RADIUS * 0.5f, 0.1f, 0f);

            Assert.IsFalse(_probe.TryFindGround(nearEdge, FLAT_RADIUS, _settings, out _));
        }

        [Test]
        public void WhenItGoesOffOnASmallThing_ThenItIsInTheAir() {
            MakeFloor(Vector3.zero, 1f, 0f);

            Assert.IsFalse(_probe.TryFindGround(_origin + Vector3.up * 0.1f, FLAT_RADIUS, _settings, out _));
        }

        [Test]
        public void WhenItGoesOffOnASteepSlope_ThenItIsInTheAir() {
            MakeFloor(Vector3.zero, 20f, 45f);

            Assert.IsFalse(_probe.TryFindGround(_origin + Vector3.up * 0.1f, FLAT_RADIUS, _settings, out _));
        }

        private void MakeFloor(Vector3 offset, float size, float tiltDegrees) {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.layer = 0;
            floor.transform.SetPositionAndRotation(_origin + offset + Vector3.down * 0.5f, Quaternion.Euler(tiltDegrees, 0f, 0f));
            floor.transform.localScale = new Vector3(size, 1f, size);
            _made.Add(floor);
            Physics.SyncTransforms();
        }
    }
}
