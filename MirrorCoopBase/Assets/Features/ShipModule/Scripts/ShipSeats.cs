using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The ship's seats: sitting down and standing up, and the helm pilot with the steer they give.
    internal sealed class ShipSeats {
        private readonly ShipBase _ship;
        private readonly ShipSocket[] _sockets;
        private readonly ShipRiders _riders;
        private readonly HelmSteer _helmSteer = new HelmSteer();

        internal ShipSeats(ShipBase ship, ShipSocket[] sockets, ShipRiders riders) {
            _ship = ship;
            _sockets = sockets;
            _riders = riders;
        }

        internal void ServerSetSteer(uint riderNetId, float lateral) {
            if (NetworkServer.active == false)
                return;

            if (IsHelmOccupant(riderNetId) == false)
                return;

            _helmSteer.Set(riderNetId, lateral);
        }

        internal bool ServerTrySit(ShipRider rider, ShipSocket socket) {
            if (NetworkServer.active == false || rider == null || socket == null)
                return false;

            if (socket.CanSeat(rider.netId) == false)
                return false;

            // A rider holds one seat: sitting down elsewhere frees the previous seat (and the helm with its steer).
            ClearOccupant(rider);
            if (socket.ServerTrySit(rider.netId) == false)
                return false;

            _riders.TrackRider(rider);

            Vector3 seatOffset = socket.ResolveSitLocalOffset(_ship.transform);
            rider.BindToSeat(_ship, seatOffset);
            bool helm = socket.Seat != null && socket.Seat.Role == ShipSeatRole.Helm;
            rider.ServerLockSeat(seatOffset, helm, _ship.NetIdentity);
            return true;
        }

        internal void ClientSyncSeat(ShipSocket socket, uint previousOccupant, uint occupant) {
            if (NetworkServer.active)
                return;

            if (TryGetOwnedRider(occupant, out ShipRider seated)) {
                _riders.TrackRider(seated);
                seated.BindToSeat(_ship, socket.ResolveSitLocalOffset(_ship.transform));
                return;
            }

            // A rider who switched seats is still seated on the other socket; only standing up releases it.
            if (_ship.IsFlying == false && IsSeatedOnShip(previousOccupant) == false && TryGetOwnedRider(previousOccupant, out ShipRider stood))
                stood.ReleaseFromPlatform();
        }

        internal void ServerStand(ShipRider rider) {
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
            if (_sockets == null || rider == null)
                return;

            if (IsHelmOccupant(rider.netId))
                _helmSteer.Clear();

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerStand(rider.netId);
            }
        }

        internal void ClearAllOccupants() {
            _helmSteer.Clear();
            if (_sockets == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearOccupant();
            }
        }

        internal void ClearSteer() =>
            _helmSteer.Clear();

        internal bool HasHelmPilot() {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null && _sockets[i].HasHelmPilot)
                    return true;
            }

            return false;
        }

        internal float ReadHelmSteer() {
            uint occupant = HelmOccupantNetId();
            if (occupant == 0)
                return 0f;

            for (int i = 0; i < _riders.All.Count; i++) {
                ShipRider rider = _riders.All[i];
                if (rider == null || rider.netId != occupant)
                    continue;

                if (rider.isOwned)
                    return rider.CurrentSteer;
            }

            return _helmSteer.Read(occupant);
        }

        private void ServerUnseat(ShipRider rider) {
            ClearOccupant(rider);
            rider.ServerUnlockSeat();
        }

        private uint HelmOccupantNetId() {
            if (_sockets == null)
                return 0;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.HasHelmPilot)
                    return socket.OccupantNetId;
            }

            return 0;
        }

        private bool IsHelmOccupant(uint riderNetId) {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.HasHelmPilot && socket.OccupantNetId == riderNetId)
                    return true;
            }

            return false;
        }

        private bool IsSeatedOnShip(uint riderNetId) {
            if (_sockets == null || riderNetId == 0)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null && _sockets[i].OccupantNetId == riderNetId)
                    return true;
            }

            return false;
        }

        private static bool TryGetOwnedRider(uint netId, out ShipRider rider) {
            rider = null;
            if (netId == 0 || NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity) == false)
                return false;

            return identity.isOwned && identity.TryGetComponent(out rider);
        }
    }
}
