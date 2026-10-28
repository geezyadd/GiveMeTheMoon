#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Overboard;
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only: where "Throw me overboard" puts the player and how far "Bring the ship" moves the ship.
    public sealed class ShipOverboardDebugPlacement : IShipOverboardDebugPlacement {
        private const float FAR_SIDE_METERS = 1000f;

        private readonly ShipOverboardConfiguration _shipOverboardConfiguration;

        public ShipOverboardDebugPlacement(ShipOverboardConfiguration shipOverboardConfiguration) =>
            _shipOverboardConfiguration = shipOverboardConfiguration;

        // The owner lets go of the deck with the ride's velocity, as after a walk off the edge, and is put beside the
        // deck's nearer side at its height, moving away from it.
        public void ThrowOverboard(ShipBase ship, ShipRider rider, Rigidbody body) {
            Vector3 local = ship.ToShipLocal(rider.transform.position);
            float side = local.x >= 0f ? 1f : -1f;
            ship.DeckGeometry.TryClosestDeckWalk(local + Vector3.right * (side * FAR_SIDE_METERS), out Vector3 edge, out _, out _);
            Vector3 thrownLocal = new Vector3(edge.x + side * _shipOverboardConfiguration.DebugThrowSideMeters, local.y, local.z);
            Vector3 away = ship.transform.right * side;
            rider.ReleaseFromPlatform();
            Vector3 thrown = ship.transform.TransformPoint(thrownLocal);
            body.position = thrown;
            body.transform.position = thrown;
            body.linearVelocity += away * _shipOverboardConfiguration.DebugThrowSpeedMetersPerSecond;
        }

        // Moved like a world shift, so the move is no velocity: the drifter ends up over the deck's middle.
        public void BringShipTo(ShipBase ship, Vector3 drifterPosition) {
            Vector3 middle = ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 standPoint) ? standPoint : Vector3.zero;
            Vector3 overDeck = ship.transform.TransformPoint(middle + Vector3.up * _shipOverboardConfiguration.DebugBringHeightMeters);
            ship.FlightControlDebug.DebugMove(drifterPosition - overDeck);
        }
    }
}
#endif
