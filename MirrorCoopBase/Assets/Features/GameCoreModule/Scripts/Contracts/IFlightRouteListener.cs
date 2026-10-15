namespace Features.GameCoreModule.Contracts {
    // The server's run tells what lives along a flight (the flight field) when a flight starts and when it is over
    // (landing, wreck, abort, end of the run); the run does not know what is built there.
    public interface IFlightRouteListener {
        public void ServerOnFlightLaunched(FlightLaunch launch);
        public void ServerOnFlightEnded();
    }
}
