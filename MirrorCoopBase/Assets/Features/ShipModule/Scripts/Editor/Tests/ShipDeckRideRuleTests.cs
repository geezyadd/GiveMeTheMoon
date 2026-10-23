using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipDeckRideRuleTests {
        // The ship's deck: 10 m wide, 19 m from the stern to the nose, the pivot well forward of the middle.
        private const float DECK_HALF_WIDTH = 5f;
        private const float STERN_Z = -10.6f;
        private const float NOSE_Z = 7.8f;
        private const float DECK_SURFACE_Y = 0.3f;
        private const float BRIDGE_SURFACE_Y = 2.5f;
        private const float DECK_INSET = 0.08f;
        // A standing player's position is this far above the deck surface.
        private const float STANDING_HEIGHT = 1f;
        private const float AT_TOP_OF_JUMP = 3f;
        private const float FALLING = -2f;
        private const float RISING = 3f;

        private ShipFlightSettings _flightSettings;

        [SetUp]
        public void SetUp() =>
            _flightSettings = ScriptableObject.CreateInstance<ShipFlightSettings>();

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_flightSettings);

        [TestCase(DECK_SURFACE_Y)]
        [TestCase(2f)]
        [TestCase(-1.5f)]
        public void WhenRiderStandsOnDeckAtAnyHeightOverPivot_ThenCatchBindsIt(float deckSurfaceY) {
            ShipDeckRideRule rule = CreateRule(FlatDeck(deckSurfaceY));

            ShipRideRefusal refusal = rule.CheckCatch(new Vector3(0f, deckSurfaceY + STANDING_HEIGHT, 0f), FALLING, DECK_INSET);

            Assert.AreEqual(ShipRideRefusal.None, refusal);
        }

        [Test]
        public void WhenRiderStandsOnRaisedSpot_ThenCatchMeasuresFromThatSurface() {
            FakeShipDeckGeometry deck = FlatDeck(DECK_SURFACE_Y).AddArea(-1f, 1f, -2f, 2f, BRIDGE_SURFACE_Y);
            ShipDeckRideRule rule = CreateRule(deck);

            ShipRideRefusal refusal = rule.CheckCatch(new Vector3(0f, BRIDGE_SURFACE_Y + STANDING_HEIGHT, 0f), FALLING, DECK_INSET);

            Assert.AreEqual(ShipRideRefusal.None, refusal);
        }

        [Test]
        public void WhenRiderIsHighAboveDeck_ThenCatchWaitsUntilItComesDown() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckCatch(new Vector3(0f, DECK_SURFACE_Y + AT_TOP_OF_JUMP, 0f), FALLING, DECK_INSET);

            Assert.AreEqual(ShipRideRefusal.AboveWindow, refusal);
        }

        [Test]
        public void WhenRiderIsStillRising_ThenCatchWaitsUntilItComesDown() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckCatch(new Vector3(0f, DECK_SURFACE_Y + STANDING_HEIGHT, 0f), RISING, DECK_INSET);

            Assert.AreEqual(ShipRideRefusal.Rising, refusal);
        }

        [Test]
        public void WhenRiderIsBesideDeck_ThenCatchRefusesIt() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckCatch(new Vector3(DECK_HALF_WIDTH + 1f, DECK_SURFACE_Y + STANDING_HEIGHT, 0f), FALLING, DECK_INSET);

            Assert.AreEqual(ShipRideRefusal.OffDeck, refusal);
        }

        [TestCase(NOSE_Z - 0.4f)]
        [TestCase(STERN_Z + 0.4f)]
        public void WhenRiderJumpsInNoseOrStern_ThenTakeoffBoardsIt(float z) {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckBoarding(new Vector3(0f, DECK_SURFACE_Y + AT_TOP_OF_JUMP, z));

            Assert.AreEqual(ShipRideRefusal.None, refusal);
        }

        [Test]
        public void WhenRiderStandsOnDeckEdgeCorner_ThenTakeoffBoardsIt() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckBoarding(new Vector3(-DECK_HALF_WIDTH - 0.3f, DECK_SURFACE_Y + STANDING_HEIGHT, STERN_Z - 0.2f));

            Assert.AreEqual(ShipRideRefusal.None, refusal);
        }

        [Test]
        public void WhenRiderIsFarOffDeck_ThenTakeoffLeavesIt() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckBoarding(new Vector3(DECK_HALF_WIDTH + 2f, DECK_SURFACE_Y + STANDING_HEIGHT, 0f));

            Assert.AreEqual(ShipRideRefusal.OffDeck, refusal);
        }

        [Test]
        public void WhenRiderIsFarAboveDeck_ThenTakeoffLeavesIt() {
            ShipDeckRideRule rule = CreateRule(FlatDeck(DECK_SURFACE_Y));

            ShipRideRefusal refusal = rule.CheckBoarding(new Vector3(0f, DECK_SURFACE_Y + _flightSettings.BoardingMaxHeight + 1f, 0f));

            Assert.AreEqual(ShipRideRefusal.AboveWindow, refusal);
        }

        private ShipDeckRideRule CreateRule(IShipDeckGeometry deck) =>
            new ShipDeckRideRule(deck, () => _flightSettings);

        private static FakeShipDeckGeometry FlatDeck(float surfaceY) =>
            new FakeShipDeckGeometry().AddArea(-DECK_HALF_WIDTH, DECK_HALF_WIDTH, STERN_Z, NOSE_Z, surfaceY);
    }
}
