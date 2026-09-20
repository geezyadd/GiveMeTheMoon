namespace Features.ShipModule.Scripts {
    public interface IFlightStatContributor {
        void Contribute(ShipSocket[] sockets, int loopIndex, FlightRunStats stats);
    }
}
