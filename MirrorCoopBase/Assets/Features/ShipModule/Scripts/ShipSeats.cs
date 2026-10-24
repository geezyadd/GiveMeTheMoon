using Features.NetworkModelModule.Scripts;
using Mirror;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Debug;
#endif

namespace Features.ShipModule.Scripts {
    // Who sits where, by the players' stable keys in the ShipSockets model. What a seated player does is its seat
    // station's business (ISeatStation), never a branch here.
    internal sealed class ShipSeats : IShipSeats {
        private readonly ShipBase _ship;
        private readonly ShipSocket[] _sockets;
        private readonly IShipRiders _riders;

        internal ShipSeats(ShipBase ship, ShipSocket[] sockets, IShipRiders riders) {
            _ship = ship;
            _sockets = sockets;
            _riders = riders;
        }

        public void ServerReceiveSeatInput(ShipRider rider, float value) {
            if (NetworkServer.active == false || TryGetHeldSocket(rider.Key, out ShipSocket socket) == false)
                return;

            socket.ServerReceiveStationInput(value);
        }

        public bool ServerTrySit(ShipRider rider, ShipSocket socket) {
            if (NetworkServer.active == false || rider == null || socket == null)
                return false;

            PlayerKey key = rider.Key;
            if (socket.CanSeat(key) == false)
                return false;

            // A rider holds one seat: sitting down elsewhere frees the previous seat (and its station).
            ClearOccupant(rider);
            if (socket.ServerTrySit(key) == false)
                return false;

            _riders.TrackRider(rider);

            Vector3 seatOffset = socket.ResolveSitLocalOffset(_ship.transform);
            rider.BindToSeat(_ship, seatOffset);
            rider.ServerLockSeat(seatOffset, _ship.NetIdentity);
            return true;
        }

        public void ClientSyncSeat(ShipSocket socket, PlayerKey previousOccupant, PlayerKey occupant) {
            if (NetworkServer.active)
                return;

            if (TryGetOwnedRider(occupant, out ShipRider seated)) {
                _riders.TrackRider(seated);
                seated.BindToSeat(_ship, socket.ResolveSitLocalOffset(_ship.transform));
                return;
            }

            // In flight the deck carries the rider on; at a station standing up releases it onto the static deck.
            if (_ship.IsFlying || IsStillSeated(previousOccupant))
                return;

            if (TryGetOwnedRider(previousOccupant, out ShipRider stood) == false)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            stood.RideDiagnostics.LogReleased(stood, _ship, ShipRideDiagnostics.CAUSE_STAND_UP);
#endif
            stood.ReleaseFromPlatform();
        }

        public void ServerStand(ShipRider rider) {
            if (NetworkServer.active == false || rider == null)
                return;

            ServerUnseat(rider);
            // At a station the deck is a static platform: the rider walks on it under physics.
            if (_ship.IsFlying == false)
                rider.ServerRelease();
        }

        internal void ServerUnseatRiders() {
            for (int i = 0; i < _riders.All.Count; i++) {
                ShipRider rider = _riders.All[i];
                if (rider != null)
                    ServerUnseat(rider);
            }
        }

        internal void ClearOccupant(ShipRider rider) {
            if (rider == null)
                return;

            PlayerKey key = rider.Key;
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerStand(key);
            }
        }

        internal void ClearAllOccupants() {
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearOccupant();
            }
        }

        private void ServerUnseat(ShipRider rider) {
            ClearOccupant(rider);
            rider.ServerUnlockSeat();
        }

        // A rider who switched seats is still seated on the other socket; only standing up releases it.
        private bool IsStillSeated(PlayerKey rider) =>
            TryGetHeldSocket(rider, out ShipSocket _);

        private bool TryGetHeldSocket(PlayerKey rider, out ShipSocket held) {
            held = null;
            if (rider.IsEmpty)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket == null || socket.IsHeldBy(rider) == false)
                    continue;

                held = socket;
                return true;
            }

            return false;
        }

        private bool TryGetOwnedRider(PlayerKey key, out ShipRider rider) {
            if (_riders.TryFindByKey(key, out rider) && rider.isOwned)
                return true;

            rider = null;
            return false;
        }
    }
}
