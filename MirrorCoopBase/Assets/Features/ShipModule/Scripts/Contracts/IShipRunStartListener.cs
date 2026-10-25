namespace Features.ShipModule.Scripts.Contracts {
    // Server: the run tells what it does not own on the ship (the tool rack) that a run has started, once the ship is
    // reset for the build phase.
    public interface IShipRunStartListener {
        public void ServerOnRunStarted();
    }
}
