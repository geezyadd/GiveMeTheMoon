#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    // Editor and development builds only: why one rider was bound to the ship (a seat) or released from it, so a
    // player who falls off a flying ship leaves a trace in the log.
    public sealed class ShipRideDiagnostics {
        public const string CAUSE_SEAT = "a seat";
        public const string CAUSE_OWNER_RIDE = "the owner's synced seat";
        public const string CAUSE_SERVER_RELEASE = "the server (landing, wreck, standing up)";
        public const string CAUSE_RIDE_ENDED = "the end of its synced ride";
        public const string CAUSE_STAND_UP = "standing up from a seat at a station";

        private const string LOG_PREFIX = "[ShipRide] ";

        public void LogBound(ShipRider rider, ShipBase ship, string cause) =>
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} bound by {cause} at {DescribeSpot(ship, rider.DebugLocalOffset)}");

        // Called right before a release: a rider that is not riding has nothing to release.
        public void LogReleased(ShipRider rider, ShipBase ship, string cause) {
            if (rider.IsRiding == false)
                return;

            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} released by {cause} at {DescribeSpot(ship, rider.DebugLocalOffset)}");
        }

        private static string DescribeRider(ShipRider rider) {
            string peer = rider.isServer ? rider.isClient ? "host" : "server" : "client";
            string owner = rider.isOwned ? "own" : "remote";
            return $"{rider.name} (netId {rider.netId}, {owner} rider on the {peer})";
        }

        // A riding player is placed by its ride offset: its transform can lag a ship that was just snapped or shifted.
        private static string DescribeSpot(ShipBase ship, Vector3 shipLocal) {
            if (ship == null)
                return $"ship-local {shipLocal:F2}, no ship";

            string height = ship.DeckGeometry.TryGetDeckSurfaceY(shipLocal, out float surfaceY)
                ? $"{shipLocal.y - surfaceY:0.00} m above the deck"
                : "no deck under it";
            return $"ship-local {shipLocal:F2}, {height}, ship flying {ship.IsFlying} on this peer";
        }
    }
}
#endif
