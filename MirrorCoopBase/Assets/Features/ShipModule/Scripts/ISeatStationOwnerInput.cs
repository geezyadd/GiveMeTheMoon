namespace Features.ShipModule.Scripts {
    // A seat station that needs a value from its seated owner on the server (the helm's steer). The owner's peer sends it
    // through its rider; the server hands it only to the station of the seat that rider holds.
    public interface ISeatStationOwnerInput {
        public void ServerReceiveOwnerInput(float value);
    }
}
