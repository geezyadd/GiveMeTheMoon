using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.FloatingControllerModule;
using Features.NetworkModelModule.Scripts;
using Mirror;

namespace Features.ShipModule.Scripts {
    // The players bound to this ship (seated) on this peer, and who is aboard: bound, or a body the ship carries.
    internal sealed class ShipRiders : IShipRiders {
        private readonly ShipBase _ship;
        // Read on use: the model is injected after the ship's parts are created, and the players spawn after the ship.
        private readonly System.Func<CharacterMovableModel> _characterMovables;
        private readonly System.Func<ShipRiderKeyModel> _riderKeys;
        private readonly List<ShipRider> _riders = new List<ShipRider>();

        private CharacterMovableBase _cachedLocalPlayer;
        private ShipRider _localRider;

        public IReadOnlyList<ShipRider> All => _riders;

        internal ShipRiders(
            ShipBase ship,
            System.Func<CharacterMovableModel> characterMovables,
            System.Func<ShipRiderKeyModel> riderKeys) {
            _ship = ship;
            _characterMovables = characterMovables;
            _riderKeys = riderKeys;
        }

        // Only leaving the ride volume matters: a player on the deck is carried by the ship, not bound by entering.
        public void SetVolumeOverlap(ShipRider rider, bool inside) {
            if (rider == null || inside)
                return;

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

        internal void FollowRiders(float dt) {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].Follow(dt);
            }
        }

        // The ship goes away with its map or is destroyed: every rider bound to it on this peer goes free.
        internal void ReleaseAllOnLeave() {
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

        // Bound to this ship (seated), or a body this ship carries (standing, walking or jumping on the deck). On a peer
        // that does not own the player, its copy rides this peer's ship when the owner's packets say so.
        public bool IsAboard(ShipRider rider) {
            if (rider == null)
                return false;

            if (rider.IsRiding)
                return _riders.Contains(rider);

            return rider.TryGetComponent(out ICarrierRider carrierRider)
                && carrierRider.Carrier != null
                && ReferenceEquals(carrierRider.Carrier, _ship.Carrier);
        }

        public bool IsLocalPlayerAboard() =>
            IsAboard(LocalRider());

        // Every player on this peer, not only the riders: a seat's occupant may stand anywhere or have just spawned.
        public bool TryFindByKey(PlayerKey key, out ShipRider rider) =>
            _riderKeys().TryGet(key, out rider);

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
