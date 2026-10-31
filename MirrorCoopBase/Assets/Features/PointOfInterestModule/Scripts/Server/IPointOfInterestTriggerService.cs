namespace Features.PointOfInterestModule.Scripts.Server {
    public interface IPointOfInterestTriggerService {
        // Server: the ship entered the point's zone; its content starts once, a later entry does nothing.
        public PointOfInterestTriggerResult ServerTrigger(int pointId);
    }
}
