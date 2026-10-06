namespace Features.ShipModule.Scripts {
    internal interface IShipSeats {
        public void ServerSetSteer(uint riderNetId, float lateral);
        public bool ServerTrySit(ShipRider rider, ShipSocket socket);
        public void ClientSyncSeat(ShipSocket socket, uint previousOccupant, uint occupant);
        public void ServerStand(ShipRider rider);
        public bool HasHelmPilot();
        public float ReadHelmSteer();
    }
}
