using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.NetworkModelModule.Scripts;
using Mirror;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Debug;
#endif

namespace Features.ShipModule.Scripts {
    internal sealed class ShipRiders : IShipRiders
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IShipRidersDebug
#endif
    {
        private readonly ShipBase _ship;
        private readonly IShipDeckRideRule _rideRule;
        // Read on use: the model is injected after the ship's parts are created, and the players spawn after the ship.
        private readonly System.Func<CharacterMovableModel> _characterMovables;
        private readonly System.Func<ShipRiderKeyModel> _riderKeys;
        private readonly List<ShipRider> _riders = new List<ShipRider>();

        private CharacterMovableBase _cachedLocalPlayer;
        private ShipRider _localRider;
        private bool _hasLeft;

        public IReadOnlyList<ShipRider> All => _riders;

        internal ShipRiders(
            ShipBase ship,
            IShipDeckRideRule rideRule,
            System.Func<CharacterMovableModel> characterMovables,
            System.Func<ShipRiderKeyModel> riderKeys) {
            _ship = ship;
            _rideRule = rideRule;
            _characterMovables = characterMovables;
            _riderKeys = riderKeys;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            TrackRider(rider);
            rider.BindToPlatform(_ship);
            rider.RideDiagnostics.LogBound(rider, _ship, ShipRideDiagnostics.CAUSE_DEBUG);
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

        // Every player over the deck rides from the takeoff, wherever on the deck it stands. Each peer binds only its own
        // player; the others follow their owner's synced ride (ShipRider hooks).
        internal void BoardRidersOverDeck() {
            if (_hasLeft)
                return;

            foreach (CharacterMovableBase movable in _characterMovables().Movables.Values) {
                if (movable == null || movable.TryGetComponent(out ShipRider rider) == false)
                    continue;

                ShipRideRefusal refusal = _rideRule.CheckBoarding(_ship.ToShipLocal(rider.transform.position));
                if (refusal != ShipRideRefusal.None) {
                    LogTakeoffRefusal(rider, refusal);
                    continue;
                }

                TrackRider(rider);
                if (rider.isOwned == false || rider.IsRiding)
                    continue;

                rider.BindToPlatform(_ship);
                LogTakeoffBound(rider);
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

        // The ship goes away with its map or is destroyed: every rider bound to it on this peer goes free, riding or
        // seated, and none is bound to it again.
        internal void ReleaseAllOnLeave() {
            _hasLeft = true;
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].ReleaseFromLeavingShip();
            }

            _riders.Clear();
        }

        public void ServerReleaseRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].ServerRelease();
            }
        }

        public bool IsLocalPlayerRiding() {
            ShipRider rider = LocalRider();
            return rider != null && rider.IsRiding;
        }

        // Every player on this peer, not only the riders: a seat's occupant may stand anywhere or have just spawned.
        public bool TryFindByKey(PlayerKey key, out ShipRider rider) =>
            _riderKeys().TryGet(key, out rider);

        private void TryCatchRider(ShipRider rider) {
            if (_hasLeft || rider == null || rider.isOwned == false || rider.IsRiding)
                return;

            ShipRideRefusal refusal = rider.CheckLanding(_ship);
            if (refusal != ShipRideRefusal.None) {
                LogCatchRefusal(rider, refusal);
                return;
            }

            TrackRider(rider);
            rider.BindToPlatform(_ship);
            LogCatchBound(rider);
        }

        private void LogTakeoffRefusal(ShipRider rider, ShipRideRefusal refusal) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (rider.isOwned)
                rider.RideDiagnostics.LogRefused(rider, _ship, ShipRideDiagnostics.CAUSE_TAKEOFF, refusal);
#endif
        }

        private void LogTakeoffBound(ShipRider rider) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (rider.IsRiding)
                rider.RideDiagnostics.LogBound(rider, _ship, ShipRideDiagnostics.CAUSE_TAKEOFF);
#endif
        }

        private void LogCatchBound(ShipRider rider) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (rider.IsRiding)
                rider.RideDiagnostics.LogBound(rider, _ship, ShipRideDiagnostics.CAUSE_CATCH);
#endif
        }

        private void LogCatchRefusal(ShipRider rider, ShipRideRefusal refusal) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            rider.RideDiagnostics.LogRefused(rider, _ship, ShipRideDiagnostics.CAUSE_CATCH, refusal);
#endif
        }

        private ShipRider LocalRider() {
            CharacterMovableBase localPlayer = _characterMovables().LocalMovable;
            if (localPlayer == _cachedLocalPlayer)
                return _localRider;

            _cachedLocalPlayer = localPlayer;
            _localRider = null;
            if (localPlayer != null)
                localPlayer.TryGetComponent(out _localRider);

            return _localRider;
        }
    }
}
