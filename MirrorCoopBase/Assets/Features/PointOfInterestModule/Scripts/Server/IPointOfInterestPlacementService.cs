namespace Features.PointOfInterestModule.Scripts.Server {
    public interface IPointOfInterestPlacementService {
        // Server: the points of the field standing now replace the old ones, each dormant with a content by weight.
        public void ServerPlaceForCurrentField();
    }
}
