using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts {
    internal interface IShipSeats {
        public void ServerReceiveSeatInput(ShipRider rider, float value);
        public bool ServerTrySit(ShipRider rider, ShipSocket socket);
        public void ClientSyncSeat(ShipSocket socket, PlayerKey previousOccupant, PlayerKey occupant);
        public void ServerStand(ShipRider rider);
    }
}
