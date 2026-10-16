using System.Diagnostics;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Editor and development builds only (the calls are compiled out of a release build): why one rider was bound to
    // the ship, released from it or refused, so a player who falls off a flying ship leaves a trace in the log. A
    // refusal is logged when its reason changes, at most once a second.
    public sealed class ShipRideDiagnostics {
        private const string EDITOR_DEFINE = "UNITY_EDITOR";
        private const string DEVELOPMENT_DEFINE = "DEVELOPMENT_BUILD";
        private const string LOG_PREFIX = "[ShipRide] ";
        private const float REFUSAL_LOG_INTERVAL_SECONDS = 1f;

        private ShipRideRefusal _lastRefusal;
        private float _nextRefusalLogAt;

        [Conditional(EDITOR_DEFINE), Conditional(DEVELOPMENT_DEFINE)]
        public void LogBound(ShipRider rider, ShipBase ship, Vector3 shipLocal, string cause) {
            _lastRefusal = ShipRideRefusal.None;
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} bound by {cause} at {DescribeSpot(ship, shipLocal)}");
        }

        [Conditional(EDITOR_DEFINE), Conditional(DEVELOPMENT_DEFINE)]
        public void LogReleased(ShipRider rider, ShipBase ship, Vector3 shipLocal, string cause) =>
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} released by {cause} at {DescribeSpot(ship, shipLocal)}");

        [Conditional(EDITOR_DEFINE), Conditional(DEVELOPMENT_DEFINE)]
        public void LogRefused(ShipRider rider, ShipBase ship, string check, ShipRideRefusal refusal) {
            if (refusal == _lastRefusal || Time.unscaledTime < _nextRefusalLogAt)
                return;

            _lastRefusal = refusal;
            _nextRefusalLogAt = Time.unscaledTime + REFUSAL_LOG_INTERVAL_SECONDS;
            Vector3 shipLocal = ship.transform.InverseTransformPoint(rider.transform.position);
            UnityEngine.Debug.Log($"{LOG_PREFIX}{DescribeRider(rider)} refused by the {check} check ({refusal}) at {DescribeSpot(ship, shipLocal)}");
        }

        private static string DescribeRider(ShipRider rider) {
            string peer = rider.isServer ? rider.isClient ? "host" : "server" : "client";
            string owner = rider.isOwned ? "own" : "remote";
            return $"{rider.name} (netId {rider.netId}, {owner} rider on the {peer})";
        }

        private static string DescribeSpot(ShipBase ship, Vector3 shipLocal) {
            string height = ship.RideRule.TryGetHeightAboveDeck(shipLocal, out float aboveDeck)
                ? $"{aboveDeck:0.00} m above the deck"
                : "no deck under it";
            return $"ship-local {shipLocal:F2}, {height}, ship flying {ship.IsFlying} on this peer";
        }
    }
}
