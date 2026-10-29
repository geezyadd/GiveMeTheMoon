namespace Features.PointOfInterestModule.Scripts.Server {
    public interface IPointOfInterestOpenService {
        // Server: the point's reward container is open (shot open or rammed): it is gone and the crew is paid, once.
        public PointOfInterestOpenResult ServerOpen(int pointId);
    }
}
