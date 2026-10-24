namespace Features.ShipModule.Scripts {
    // What a seat's module does with the player sitting in it (a turret: aim and fire). The socket tells the station
    // on every peer who sits there; the station takes the owner's input itself. A seat without a station only seats.
    public interface ISeatStation {
        public void Enter(uint occupantNetId);

        public void Exit();
    }
}
