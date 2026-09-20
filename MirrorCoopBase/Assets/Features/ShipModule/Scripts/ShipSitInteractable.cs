using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSitInteractable : InteractableBase {
        [SerializeField] private ShipSocket _socket;
        [SerializeField] private ShipBase _ship;

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || _socket.IsSittable == false)
                return false;

            if (grab != null && grab.IsHolding)
                return false;

            uint occupant = _socket.OccupantNetId;
            return occupant == 0 || (user != null && occupant == user.netId);
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || _ship == null || user == null)
                return;

            ShipRider rider = user.GetComponent<ShipRider>();
            if (rider == null)
                return;

            if (_socket.OccupantNetId == rider.netId)
                _ship.ServerStand(rider);
            else
                _ship.ServerTrySit(rider, _socket);
        }
    }
}
