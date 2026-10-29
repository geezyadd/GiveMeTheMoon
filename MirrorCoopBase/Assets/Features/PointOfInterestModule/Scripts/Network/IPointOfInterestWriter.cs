namespace Features.PointOfInterestModule.Scripts.Network {
    // Server: the one way the points of interest change.
    public interface IPointOfInterestWriter {
        // The server's ship identity is up, so the points can be written.
        public bool IsAvailable { get; }
        public void ServerAdd(PointOfInterestEntry entry);
        public void ServerSet(int index, PointOfInterestEntry entry);
        public void ServerClear();
    }
}
