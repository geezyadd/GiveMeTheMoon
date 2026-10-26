using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Where a seated rider stands up. A moving seat point (a turret's) may have carried the rider past the deck edge, so
    // that spot is clamped onto the deck walk; a fixed seat (the helm, a passenger seat) keeps its spot as before.
    public sealed class SeatStandingSpot {
        public bool IsFromMovingSeat { get; private set; }

        public void Track(bool isFollowingSeatPoint) =>
            IsFromMovingSeat = isFollowingSeatPoint;

        public bool TryResolve(Vector3 seatedOffset, IShipDeckGeometry deckGeometry, float deckInset, out Vector3 standingOffset) {
            standingOffset = seatedOffset;
            if (IsFromMovingSeat == false)
                return false;

            deckGeometry.ClampDeckWalk(ref standingOffset, deckInset);
            return true;
        }
    }
}
