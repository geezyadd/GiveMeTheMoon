using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts.Editor.Tests {
    // A rock is touched at its kind's share of the data sphere, not at the whole sphere: the sphere holds the turned
    // mesh with room to spare, so a hull inside it but outside the share passes the visible rock with a gap.
    public sealed class ShipHullRockScannerTests {
        private const string SOURCE_ID = "test_rock";
        private const float SMALL_DIAMETER = 20f;
        private const float LARGE_DIAMETER = 60f;
        // How far inside / outside the contact sphere the rock's middle is put from the hull's side (m).
        private const float CLEARANCE = 0.5f;
        private static readonly ShipHullBox _hull = new() { Center = Vector3.zero, HalfExtents = new Vector3(2f, 1f, 5f) };
        private static readonly Pose _still = new(Vector3.zero, Quaternion.identity);

        private ShipRockImpactConfiguration _configuration;
        private FieldRockQueryStub _field;
        private ShipHullRockScanner _scanner;
        private List<ShipRockContact> _contacts;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipRockImpactConfiguration>();
            _field = new FieldRockQueryStub();
            _scanner = new ShipHullRockScanner(_field, new ShipRockImpactClassifier(), new ShipRockContactRule(_configuration), _configuration);
            _contacts = new List<ShipRockContact>();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenTheHullIsInsideASmallRocksDataSphere_AndOutsideItsContactShare_ThenTheRockIsNotHit() {
            float contactRadius = SMALL_DIAMETER * 0.5f * _configuration.SmallContactRadiusShare;
            _field.Add(CreateBesideHull(SMALL_DIAMETER, FieldRockSize.Small, contactRadius + CLEARANCE));

            Scan();

            Assert.IsEmpty(_contacts, "The hull hit a small rock it only passes by inside the rock's data sphere.");
        }

        [Test]
        public void WhenTheHullReachesASmallRocksContactShare_ThenTheRockIsHit() {
            float contactRadius = SMALL_DIAMETER * 0.5f * _configuration.SmallContactRadiusShare;
            _field.Add(CreateBesideHull(SMALL_DIAMETER, FieldRockSize.Small, contactRadius - CLEARANCE));

            Scan();

            Assert.AreEqual(1, _contacts.Count, "The hull did not hit a small rock it reaches.");
            Assert.AreEqual(ShipRockImpactKind.Small, _contacts[0].Kind);
        }

        [Test]
        public void WhenTheHullIsInsideALargeRocksDataSphere_AndOutsideItsContactShareAndMargin_ThenTheRockIsNotTouched() {
            float contactRadius = LARGE_DIAMETER * 0.5f * _configuration.LargeContactRadiusShare + _configuration.LargeContactMarginMeters;
            _field.Add(CreateBesideHull(LARGE_DIAMETER, FieldRockSize.Large, contactRadius + CLEARANCE));

            Scan();

            Assert.IsEmpty(_contacts, "The hull touched a large rock it only passes by inside the rock's data sphere.");
        }

        [Test]
        public void WhenTheHullReachesALargeRocksContactShare_ThenTheRockIsTouched() {
            float contactRadius = LARGE_DIAMETER * 0.5f * _configuration.LargeContactRadiusShare;
            _field.Add(CreateBesideHull(LARGE_DIAMETER, FieldRockSize.Large, contactRadius - CLEARANCE));

            Scan();

            Assert.AreEqual(1, _contacts.Count, "The hull did not touch a large rock it reaches.");
            Assert.AreEqual(ShipRockImpactKind.Large, _contacts[0].Kind);
        }

        private void Scan() =>
            _scanner.Scan(new[] { _hull }, _still, _still, Vector3.zero, _contacts);

        // The rock's middle this far out from the hull's right side; its data sphere always reaches into the hull.
        private static FieldRockDescription CreateBesideHull(float diameter, FieldRockSize size, float distanceFromSide) {
            Assert.Less(distanceFromSide, diameter * 0.5f, "The test rock's data sphere does not reach the hull.");
            Vector3 center = new(_hull.HalfExtents.x + distanceFromSide, 0f, 0f);
            return new FieldRockDescription(FieldRockId.CreateDebris(Vector2Int.one, 0), SOURCE_ID, center, diameter, size, true);
        }
    }
}
