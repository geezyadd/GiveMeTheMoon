using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipRiders : IShipRiders
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IShipRidersDebug
#endif
    {
        private const string TAKEOFF_CHECK = "takeoff";
        private const string CATCH_CHECK = "in-flight catch";

        private readonly ShipBase _ship;
        private readonly ShipDeckRideRule _rideRule;
        // Read on use: the local player can spawn after the ship.
        private readonly System.Func<CharacterMovableBase> _localPlayer;
        private readonly List<ShipRider> _riders = new List<ShipRider>();

        private CharacterMovableBase _cachedLocalPlayer;
        private ShipRider _localRider;

        public IReadOnlyList<ShipRider> All => _riders;

        internal ShipRiders(ShipBase ship, ShipDeckRideRule rideRule, System.Func<CharacterMovableBase> localPlayer) {
            _ship = ship;
            _rideRule = rideRule;
            _localPlayer = localPlayer;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            TrackRider(rider);
            rider.BindToPlatform(_ship);
        }
#endif

        public void SetVolumeOverlap(ShipRider rider, bool inside) {
            if (rider == null)
                return;

            if (inside) {
                if (_ship.IsFlying)
                    TryCatchRider(rider);

                return;
            }

            // Binding turns the rider's collisions off, which raises this exit; a bound rider is released by its own path.
            if (rider.IsRiding)
                return;

            UnregisterRider(rider);
        }

        public void UnregisterRider(ShipRider rider) {
            if (_ship.IsFlying)
                return;

            if (rider != null && NetworkServer.active)
                rider.ServerRelease();

            _riders.Remove(rider);
        }

        public void TrackRider(ShipRider rider) {
            if (_riders.Contains(rider) == false)
                _riders.Add(rider);
        }

        internal void Remove(ShipRider rider) =>
            _riders.Remove(rider);

        // Every rider over the deck rides from the takeoff, wherever on the deck it stands. Each peer binds only its own
        // player; the others follow their owner's synced ride (ShipRider hooks).
        internal void BoardRidersOverDeck() {
            ShipRider[] riders = Object.FindObjectsByType<ShipRider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < riders.Length; i++) {
                ShipRider rider = riders[i];
                ShipRideRefusal refusal = _rideRule.CheckBoarding(ToShipLocal(rider.transform.position));
                if (refusal != ShipRideRefusal.None) {
                    if (rider.isOwned)
                        rider.LogRideRefusal(_ship, TAKEOFF_CHECK, refusal);

                    continue;
                }

                TrackRider(rider);
                if (rider.isOwned)
                    rider.BindToPlatform(_ship);
            }
        }

        // Every frame in flight, not only on the ride volume's trigger enter: a player standing or coming down on the
        // deck rides within a frame, also one who never entered the trigger (spawned inside it, collisions turned back on).
        internal void CatchDeckRiders() =>
            TryCatchRider(LocalRider());

        internal void FollowRiders(float dt) {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].Follow(dt);
            }
        }

        public void ServerReleaseRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].ServerRelease();
            }
        }

        internal bool HasOwnedHelmPilot() {
            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                if (rider != null && rider.isOwned && rider.IsHelmSeat)
                    return true;
            }

            return false;
        }

        private void TryCatchRider(ShipRider rider) {
            if (rider == null || rider.isOwned == false || rider.IsRiding)
                return;

            ShipRideRefusal refusal = rider.CheckLanding(_ship);
            if (refusal != ShipRideRefusal.None) {
                rider.LogRideRefusal(_ship, CATCH_CHECK, refusal);
                return;
            }

            TrackRider(rider);
            rider.BindToPlatform(_ship);
        }

        private ShipRider LocalRider() {
            CharacterMovableBase localPlayer = _localPlayer();
            if (localPlayer == _cachedLocalPlayer)
                return _localRider;

            _cachedLocalPlayer = localPlayer;
            _localRider = null;
            if (localPlayer != null)
                localPlayer.TryGetComponent(out _localRider);

            return _localRider;
        }

        private Vector3 ToShipLocal(Vector3 worldPosition) {
            Transform ship = _ship.transform;
            return Quaternion.Inverse(ship.rotation) * (worldPosition - ship.position);
        }
    }
}
