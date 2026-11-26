using UnityEngine;

namespace Features.ShipModule.Scripts.Riding {
    // A seated rider's place: a seat point that moves with its station (a turret's yaw) is followed on every peer, and
    // standing up finds a free spot on the deck next to where the rider sat.
    internal sealed class ShipSeatRide {
        private readonly SeatStandingSpot _standingSpot = new SeatStandingSpot();

        private Transform _seatPoint;

        public bool IsFollowingSeatPoint(bool seated) =>
            seated && _seatPoint != null;

        public void FollowSeatPoint(Transform seatPoint) =>
            _seatPoint = seatPoint;

        public void StopFollowingSeatPoint(Transform seatPoint) {
            if (ReferenceEquals(_seatPoint, seatPoint))
                _seatPoint = null;
        }

        // The seat point's place in the platform's axes.
        public Vector3 SeatPointOffset(Transform platform) =>
            Quaternion.Inverse(platform.rotation) * (_seatPoint.position - platform.position);

        public void Track(bool isFollowingSeatPoint) =>
            _standingSpot.Track(isFollowingSeatPoint);

        public bool TryResolveStandingSpot(Vector3 seatedOffset, IShipDeckGeometry deckGeometry, float deckInset, out Vector3 standingOffset) =>
            _standingSpot.TryResolve(seatedOffset, deckGeometry, deckInset, out standingOffset);
    }
}
