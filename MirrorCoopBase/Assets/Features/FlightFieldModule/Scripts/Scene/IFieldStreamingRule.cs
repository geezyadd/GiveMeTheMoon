namespace Features.FlightFieldModule.Scripts.Scene {
    // When a placement near the viewpoint is built or released; distances are squared, on the flight plane.
    public interface IFieldStreamingRule {
        public bool ShouldBuild(float distanceSquared);
        public bool ShouldRelease(float distanceSquared);
    }
}
