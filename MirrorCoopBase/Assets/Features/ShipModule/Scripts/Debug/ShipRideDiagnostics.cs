#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    // Editor and development builds only: why one rider was bound to the ship, released from it or refused, so a player
    // who falls off a flying ship leaves a trace in the log. A refusal is logged when its reason changes, at most once a
    // second.
    public sealed class ShipRideDiagnostics {
        public const string CAUSE_TAKEOFF = "the takeoff boarding";
        public const string CAUSE_CATCH = "the in-flight catch";
        public const string CAUSE_SEAT = "a seat";
        public const string CAUSE_OWNER_RIDE = "the owner's synced ride";
        public const string CAUSE_DEBUG = "a debug bind";
        public const string CAUSE_SERVER_RELEASE = "the server (landing, wreck, standing up or the deck edge)";
        public const string CAUSE_DECK_EDGE = "walking off the deck edge";
        public const string CAUSE_RIDE_ENDED = "the end of its synced ride";
        public const string CAUSE_STAND_UP = "standing up from a seat at a station";

        private const string LOG_PREFIX = "[ShipRide] ";
        private const float REFUSAL_LOG_INTERVAL_SECONDS = 1f;

        private ShipRideRefusal _lastRefusal;
        private float _nextRefusalLogAt;

        public void LogBound(ShipRider rider, ShipBase ship, string cause) {
            _lastRefusal = ShipRideRefusal.None;
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} bound by {cause} at {DescribeSpot(ship, rider.DebugLocalOffset)}");
        }

        // Called right before a release: a rider that is not riding has nothing to release.
        public void LogReleased(ShipRider rider, ShipBase ship, string cause) {
            if (rider.IsRiding == false)
                return;

            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} released by {cause} at {DescribeSpot(ship, rider.DebugLocalOffset)}");
        }

        public void LogRefused(ShipRider rider, ShipBase ship, string check, ShipRideRefusal refusal) {
            if (refusal == _lastRefusal || Time.unscaledTime < _nextRefusalLogAt)
                return;

            _lastRefusal = refusal;
            _nextRefusalLogAt = Time.unscaledTime + REFUSAL_LOG_INTERVAL_SECONDS;
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} refused by {check} ({refusal}) at {DescribeSpot(ship, ship.ToShipLocal(rider.transform.position))}");
        }

        private static string DescribeRider(ShipRider rider) {
            string peer = rider.isServer ? rider.isClient ? "host" : "server" : "client";
            string owner = rider.isOwned ? "own" : "remote";
            return $"{rider.name} (netId {rider.netId}, {owner} rider on the {peer})";
        }

        // A riding player is placed by its ride offset: its transform can lag a ship that was just snapped or shifted.
        private static string DescribeSpot(ShipBase ship, Vector3 shipLocal) {
            string height = ship.RideRule.TryGetHeightAboveDeck(shipLocal, out float aboveDeck)
                ? $"{aboveDeck:0.00} m above the deck"
                : "no deck under it";
            return $"ship-local {shipLocal:F2}, {height}, ship flying {ship.IsFlying} on this peer";
        }
    }
}
#endif
