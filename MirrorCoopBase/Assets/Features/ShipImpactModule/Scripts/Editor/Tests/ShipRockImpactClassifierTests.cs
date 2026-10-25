using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts.Editor.Tests {
    public sealed class ShipRockImpactClassifierTests {
        private const string SOURCE_ID = "test_rock";
        private const float DIAMETER = 20f;

        private readonly ShipRockImpactClassifier _classifier = new ShipRockImpactClassifier();

        [TestCase(FieldRockSize.Small, true, ShipRockImpactKind.Small)]
        [TestCase(FieldRockSize.Small, false, ShipRockImpactKind.Large)]
        [TestCase(FieldRockSize.Large, true, ShipRockImpactKind.Large)]
        [TestCase(FieldRockSize.Large, false, ShipRockImpactKind.Large)]
        public void WhenRockDataIsRead_ThenTheHitKindFollowsItsSizeAndBreakability(FieldRockSize size, bool isBreakable, ShipRockImpactKind expected) {
            FieldRockDescription rock = new FieldRockDescription(FieldRockId.CreateDebris(Vector2Int.one, 0), SOURCE_ID, Vector3.zero, DIAMETER, size, isBreakable);

            Assert.AreEqual(expected, _classifier.Classify(rock));
        }
    }
}
