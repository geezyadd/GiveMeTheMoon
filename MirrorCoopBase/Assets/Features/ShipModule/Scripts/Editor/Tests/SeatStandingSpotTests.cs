using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // Standing up from a turret, whose seat point turns with the gun, clamps the spot onto the deck walk; standing up
    // from a fixed seat such as the helm keeps the spot as it was.
    public sealed class SeatStandingSpotTests {
        private const float DECK_INSET = 0.08f;
        private const float DECK_EDGE_Z = 3f;
        private const float TOLERANCE = 0.0001f;

        private static readonly Vector3 _pastDeckEdge = new Vector3(-2.6f, 1.3f, 3.35f);
        private static readonly Vector3 _onDeck = new Vector3(-2.6f, 1.3f, 1.5f);

        private FakeShipDeckGeometry _deck;

        [SetUp]
        public void SetUp() =>
            _deck = new FakeShipDeckGeometry().AddArea(-4f, 4f, -6f, DECK_EDGE_Z, 0.3f);

        [Test]
        public void WhenRiderFollowedTurretSeatPastDeckEdge_AndStands_ThenSpotIsClampedOntoDeck() {
            SeatStandingSpot spot = new SeatStandingSpot();
            spot.Track(true);

            bool isMoved = spot.TryResolve(_pastDeckEdge, _deck, DECK_INSET, out Vector3 standingOffset);

            Assert.IsTrue(isMoved);
            Assert.AreEqual(DECK_EDGE_Z, standingOffset.z, TOLERANCE);
            Assert.AreEqual(_pastDeckEdge.x, standingOffset.x, TOLERANCE);
            Assert.AreEqual(_pastDeckEdge.y, standingOffset.y, TOLERANCE);
        }

        [Test]
        public void WhenRiderSatAtHelmPastDeckEdge_AndStands_ThenSpotIsUnchanged() {
            SeatStandingSpot spot = new SeatStandingSpot();
            spot.Track(false);

            bool isMoved = spot.TryResolve(_pastDeckEdge, _deck, DECK_INSET, out Vector3 standingOffset);

            Assert.IsFalse(isMoved);
            Assert.AreEqual(_pastDeckEdge, standingOffset);
        }

        [Test]
        public void WhenRiderMovedFromTurretToHelm_AndStands_ThenSpotIsUnchanged() {
            SeatStandingSpot spot = new SeatStandingSpot();
            spot.Track(true);
            spot.Track(false);

            bool isMoved = spot.TryResolve(_pastDeckEdge, _deck, DECK_INSET, out Vector3 standingOffset);

            Assert.IsFalse(isMoved);
            Assert.AreEqual(_pastDeckEdge, standingOffset);
        }

        [Test]
        public void WhenRiderFollowedTurretSeatOnDeck_AndStands_ThenSpotStaysWhereItWas() {
            SeatStandingSpot spot = new SeatStandingSpot();
            spot.Track(true);

            spot.TryResolve(_onDeck, _deck, DECK_INSET, out Vector3 standingOffset);

            Assert.AreEqual(_onDeck, standingOffset);
        }
    }
}
