namespace Features.FlightFieldModule.Contracts {
    // Every peer, the server included: the rocks of the field this peer has from the network (the published placements
    // and debris, the synced broken list), with the same ids and poses as IFieldRockQuery on the server once the field
    // is published. For what a peer shows (the sonar); the server's checks keep IFieldRockQuery.
    public interface IPeerFieldRockQuery : IFieldRockQuery {
    }
}
