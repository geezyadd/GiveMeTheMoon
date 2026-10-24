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

            return _socket.HasOccupant == false || IsSeatedHere(user);
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || _ship == null || user == null)
                return;

            ShipRider rider = user.GetComponent<ShipRider>();
            if (rider == null)
                return;

            if (_socket.IsHeldBy(rider.Key))
                _ship.Seats.ServerStand(rider);
            else
                _ship.Seats.ServerTrySit(rider, _socket);
        }

        private bool IsSeatedHere(NetworkIdentity user) =>
            user != null && user.TryGetComponent(out ShipRider rider) && _socket.IsHeldBy(rider.Key);
    }
}
