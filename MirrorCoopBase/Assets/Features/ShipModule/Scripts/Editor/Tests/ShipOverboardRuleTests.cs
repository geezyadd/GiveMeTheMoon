using Features.ShipModule.Scripts.Overboard;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipOverboardRuleTests {
        private const float DECK_HALF_WIDTH = 5f;
        private const float STERN_Z = -10f;
        private const float NOSE_Z = 8f;
        private const float DECK_SURFACE_Y = 0.3f;
        private const float STANDING_HEIGHT = 1f;
        private const float A_LITTLE = 0.2f;
        private const float NEAR_SHIP_METERS = 20f;
        private const float LONG_ENOUGH_SECONDS = 5f;

        private ShipOverboardConfiguration _configuration;
        private ShipOverboardRule _rule;
        private FakeShipDeckGeometry _deck;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipOverboardConfiguration>();
            _rule = new ShipOverboardRule(_configuration);
            _deck = new FakeShipDeckGeometry().AddArea(-DECK_HALF_WIDTH, DECK_HALF_WIDTH, STERN_Z, NOSE_Z, DECK_SURFACE_Y);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenPlayerStandsOnDeck_ThenItIsInShipVolume() =>
            Assert.IsTrue(_rule.IsInShipVolume(_deck, new Vector3(0f, DECK_SURFACE_Y + STANDING_HEIGHT, 0f)));

        [Test]
        public void WhenPlayerIsJustBesideDeckEdge_ThenItIsStillInShipVolume() {
            float x = DECK_HALF_WIDTH + _configuration.VolumeEdgeMeters - A_LITTLE;

            Assert.IsTrue(_rule.IsInShipVolume(_deck, new Vector3(x, DECK_SURFACE_Y + STANDING_HEIGHT, 0f)));
        }

        [Test]
        public void WhenPlayerIsPastDeckEdge_ThenItIsOutOfShipVolume() {
            float x = DECK_HALF_WIDTH + _configuration.VolumeEdgeMeters + A_LITTLE;

            Assert.IsFalse(_rule.IsInShipVolume(_deck, new Vector3(x, DECK_SURFACE_Y + STANDING_HEIGHT, 0f)));
        }

        [Test]
        public void WhenPlayerIsBelowDeckBand_ThenItIsOutOfShipVolume() {
            float y = DECK_SURFACE_Y + _configuration.VolumeMinHeightMeters - A_LITTLE;

            Assert.IsFalse(_rule.IsInShipVolume(_deck, new Vector3(0f, y, 0f)));
        }

        [Test]
        public void WhenPlayerIsAboveDeckBand_ThenItIsOutOfShipVolume() {
            float y = DECK_SURFACE_Y + _configuration.VolumeMaxHeightMeters + A_LITTLE;

            Assert.IsFalse(_rule.IsInShipVolume(_deck, new Vector3(0f, y, 0f)));
        }

        [Test]
        public void WhenPlayerRidesInFlightForFirstTime_ThenItsRideIsMarked() =>
            Assert.AreEqual(ShipOverboardDecision.MarkRidden, _rule.Decide(Aboard(isFlying: true, hasRidden: false, isRiding: true, isInVolume: true)));

        [Test]
        public void WhenRiderLeavesShipVolumeInFlight_ThenItGoesOverboard() =>
            Assert.AreEqual(ShipOverboardDecision.GoOverboard, _rule.Decide(Aboard(isFlying: true, hasRidden: true, isRiding: false, isInVolume: false)));

        [Test]
        public void WhenRiderFallsOffEdgeButIsStillInShipVolume_ThenItIsNotOverboardYet() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Aboard(isFlying: true, hasRidden: true, isRiding: false, isInVolume: true)));

        [Test]
        public void WhenPlayerLeftBehindAtStationIsOutOfShipVolume_ThenItDoesNotGoOverboard() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Aboard(isFlying: true, hasRidden: false, isRiding: false, isInVolume: false)));

        [Test]
        public void WhenShipStandsAtStation_ThenNobodyGoesOverboardAndTheRideIsForgotten() =>
            Assert.AreEqual(ShipOverboardDecision.ForgetRide, _rule.Decide(Aboard(isFlying: false, hasRidden: true, isRiding: false, isInVolume: false)));

        [Test]
        public void WhenDrifterEntersShipVolumeAfterMinimumDrift_ThenShipCapturesIt() =>
            Assert.AreEqual(ShipOverboardDecision.Capture, _rule.Decide(Drifting(isRiding: false, isInVolume: true, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenDrifterIsStillInShipVolumeRightAfterLeaving_ThenItIsNotCapturedYet() {
            float justLeft = _configuration.MinDriftSecondsBeforeCapture * 0.5f;

            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Drifting(isRiding: false, isInVolume: true, justLeft, NEAR_SHIP_METERS)));
        }

        [Test]
        public void WhenDeckCatchBoundDrifter_ThenDriftEndsOnDeck() =>
            Assert.AreEqual(ShipOverboardDecision.EndOnDeck, _rule.Decide(Drifting(isRiding: true, isInVolume: true, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenDrifterIsFartherThanLimit_ThenItExpires() {
            float tooFar = _configuration.MaxDistanceMeters + 1f;

            Assert.AreEqual(ShipOverboardDecision.Expire, _rule.Decide(Drifting(isRiding: false, isInVolume: false, LONG_ENOUGH_SECONDS, tooFar)));
        }

        [Test]
        public void WhenDrifterDriftsLongerThanLimit_ThenItExpires() {
            float tooLong = _configuration.MaxDriftSeconds + 1f;

            Assert.AreEqual(ShipOverboardDecision.Expire, _rule.Decide(Drifting(isRiding: false, isInVolume: false, tooLong, NEAR_SHIP_METERS)));
        }

        [Test]
        public void WhenDrifterIsWithinLimits_ThenItDriftsOn() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Drifting(isRiding: false, isInVolume: false, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenRopeHolderKeepsWeaponWithinRope_ThenRopeHolds() =>
            Assert.IsTrue(_rule.IsRopeHeld(true, 10f, 80f));

        [Test]
        public void WhenRopeHolderGoesBeyondRopeOrLetsGo_ThenRopeBreaks() {
            Assert.IsFalse(_rule.IsRopeHeld(true, 81f, 80f));
            Assert.IsFalse(_rule.IsRopeHeld(false, 10f, 80f));
        }

        private static ShipOverboardState Aboard(bool isFlying, bool hasRidden, bool isRiding, bool isInVolume) =>
            new ShipOverboardState(isFlying, false, hasRidden, isRiding, isInVolume, 0f, 0f);

        private static ShipOverboardState Drifting(bool isRiding, bool isInVolume, float driftSeconds, float distanceMeters) =>
            new ShipOverboardState(true, true, true, isRiding, isInVolume, driftSeconds, distanceMeters);
    }
}
