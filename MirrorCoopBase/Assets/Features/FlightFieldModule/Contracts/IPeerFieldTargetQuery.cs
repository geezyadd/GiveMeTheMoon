namespace Features.FlightFieldModule.Contracts {
    // Every peer, the server included: the target balloons of the field this peer has from the network (the published
    // balloons, the synced popped list), by the same ids as IFieldTargetQuery on the server. For what a peer shows (the
    // sonar); the server's checks keep IFieldTargetQuery.
    public interface IPeerFieldTargetQuery : IFieldTargetQuery {
    }
}
