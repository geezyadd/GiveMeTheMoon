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
            Assert.AreEqual(ShipOverboardDecision.MarkInside, _rule.Decide(Aboard(isFlying: true, hasRidden: false, isRiding: true, isInVolume: true)));

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
        public void WhenPlayerOnGroundIsInAreaForFirstTime_ThenItIsMarkedInside() =>
            Assert.AreEqual(ShipOverboardDecision.MarkInside, _rule.Decide(Aboard(isFlying: false, hasRidden: false, isRiding: false, isInVolume: true)));

        [Test]
        public void WhenPlayerOnGroundLeavesEveryArea_ThenItGoesOverboard() =>
            Assert.AreEqual(ShipOverboardDecision.GoOverboard, _rule.Decide(Aboard(isFlying: false, hasRidden: true, isRiding: false, isInVolume: false)));

        [Test]
        public void WhenPlayerOnGroundWasNeverInside_ThenItDoesNotGoOverboard() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Aboard(isFlying: false, hasRidden: false, isRiding: false, isInVolume: false)));

        [Test]
        public void WhenGroundDrifterIsBackInAreaAfterAMoment_ThenDriftEnds() =>
            Assert.AreEqual(ShipOverboardDecision.EndDrift, _rule.Decide(DriftingOnGround(isInArea: true, isOverFloor: true, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenGroundDrifterIsBackInAreaRightAfterLeaving_ThenItDriftsOn() {
            float justLeft = _configuration.MinDriftSecondsBeforeCapture * 0.5f;

            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(DriftingOnGround(isInArea: true, isOverFloor: true, justLeft, NEAR_SHIP_METERS)));
        }

        [Test]
        public void WhenGroundDrifterIsFartherThanAreaLimit_ThenItIsPutBack() {
            float tooFar = _configuration.AreaMaxDistanceMeters + 1f;

            Assert.AreEqual(ShipOverboardDecision.ReturnToArea, _rule.Decide(DriftingOnGround(isInArea: false, isOverFloor: false, LONG_ENOUGH_SECONDS, tooFar)));
        }

        [Test]
        public void WhenGroundDrifterDriftsLongerThanAreaLimit_ThenItIsPutBack() {
            float tooLong = _configuration.AreaMaxDriftSeconds + 1f;

            Assert.AreEqual(ShipOverboardDecision.ReturnToArea, _rule.Decide(DriftingOnGround(isInArea: false, isOverFloor: false, tooLong, NEAR_SHIP_METERS)));
        }

        [Test]
        public void WhenGroundDrifterIsInAreaBesideFloorButNotOverIt_ThenItDriftsOn() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(DriftingOnGround(isInArea: true, isOverFloor: false, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenPointIsOverFloorAboveItsTop_ThenItIsOverFloor() {
            Bounds floor = new Bounds(Vector3.zero, new Vector3(10f, 0.4f, 10f));

            Assert.IsTrue(_rule.IsOverFloor(floor, new Vector3(4f, 0.2f + STANDING_HEIGHT, 0f)));
        }

        [Test]
        public void WhenPointIsBesideFloorOrUnderItsTop_ThenItIsNotOverFloor() {
            Bounds floor = new Bounds(Vector3.zero, new Vector3(10f, 0.4f, 10f));

            Assert.IsFalse(_rule.IsOverFloor(floor, new Vector3(5f + A_LITTLE, 0.2f + STANDING_HEIGHT, 0f)));
            Assert.IsFalse(_rule.IsOverFloor(floor, new Vector3(0f, 0.2f - A_LITTLE, 0f)));
        }
        [Test]
        public void WhenPointIsJustBesideFloorEdge_ThenItIsInArea() {
            Bounds floor = new Bounds(Vector3.zero, new Vector3(10f, 0.4f, 10f));
            float x = 5f + _configuration.VolumeEdgeMeters - A_LITTLE;

            Assert.IsTrue(_rule.IsInArea(floor, new Vector3(x, 0.2f + STANDING_HEIGHT, 0f)));
        }

        [Test]
        public void WhenPointIsPastFloorEdgeOrBelowBand_ThenItIsOutOfArea() {
            Bounds floor = new Bounds(Vector3.zero, new Vector3(10f, 0.4f, 10f));
            float x = 5f + _configuration.VolumeEdgeMeters + A_LITTLE;
            float y = 0.2f + _configuration.VolumeMinHeightMeters - A_LITTLE;

            Assert.IsFalse(_rule.IsInArea(floor, new Vector3(x, 0.2f + STANDING_HEIGHT, 0f)));
            Assert.IsFalse(_rule.IsInArea(floor, new Vector3(0f, y, 0f)));
        }

        [Test]
        public void WhenDrifterIsBesideShipButNotAboard_ThenItDriftsOn() =>
            Assert.AreEqual(ShipOverboardDecision.None, _rule.Decide(Drifting(isRiding: false, isInVolume: true, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        [Test]
        public void WhenDrifterBoardsDeck_ThenDriftEndsOnDeck() =>
            Assert.AreEqual(ShipOverboardDecision.EndDrift, _rule.Decide(Drifting(isRiding: true, isInVolume: true, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

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
        public void WhenLooseItemIsInSafeSpace_ThenItStaysAsItIs() =>
            Assert.AreEqual(ShipOverboardItemDecision.None, _rule.DecideItem(new ShipOverboardItemState(false, true, true, 0f, 0f)));

        [Test]
        public void WhenLooseItemLeavesSafeSpace_ThenItBeginsToDrift() =>
            Assert.AreEqual(ShipOverboardItemDecision.BeginDrift, _rule.DecideItem(new ShipOverboardItemState(false, false, false, 0f, 3f)));

        [Test]
        public void WhenDriftingItemIsOverFloorLongEnough_ThenItsDriftEnds() =>
            Assert.AreEqual(ShipOverboardItemDecision.EndDrift,
                _rule.DecideItem(new ShipOverboardItemState(true, true, true, LONG_ENOUGH_SECONDS, 0f)));

        [Test]
        public void WhenDriftingItemIsOverFloorTooSoon_ThenItDriftsOn() =>
            Assert.AreEqual(ShipOverboardItemDecision.None, _rule.DecideItem(new ShipOverboardItemState(true, true, true, 0f, 0f)));

        [Test]
        public void WhenDriftingItemIsFartherThanLimit_ThenItIsRemoved() {
            float tooFar = _configuration.ItemMaxDistanceMeters + 1f;

            Assert.AreEqual(ShipOverboardItemDecision.Remove,
                _rule.DecideItem(new ShipOverboardItemState(true, false, false, LONG_ENOUGH_SECONDS, tooFar)));
        }

        [Test]
        public void WhenDriftingItemIsWithinLimit_ThenItDriftsOn() =>
            Assert.AreEqual(ShipOverboardItemDecision.None,
                _rule.DecideItem(new ShipOverboardItemState(true, false, false, LONG_ENOUGH_SECONDS, NEAR_SHIP_METERS)));

        private static ShipOverboardState Aboard(bool isFlying, bool hasRidden, bool isRiding, bool isInVolume) =>
            new ShipOverboardState(isFlying, false, hasRidden, isRiding, isInVolume, 0f, 0f);

        private static ShipOverboardState Drifting(bool isRiding, bool isInVolume, float driftSeconds, float distanceMeters) =>
            new ShipOverboardState(true, true, true, isRiding, isInVolume, driftSeconds, distanceMeters);

        private static ShipOverboardState DriftingOnGround(bool isInArea, bool isOverFloor, float driftSeconds, float distanceMeters) =>
            new ShipOverboardState(false, true, true, false, isInArea, driftSeconds, distanceMeters, isOverFloor);
    }
}
