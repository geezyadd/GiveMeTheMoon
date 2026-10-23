using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Scene {
    // The release radius is above the build radius, so a structure at the edge is not rebuilt every frame.
    public sealed class FieldStreamingRule : IFieldStreamingRule {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FieldStreamingRule(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public bool ShouldBuild(float distanceSquared) =>
            distanceSquared <= _flightFieldConfiguration.BuildRadius * _flightFieldConfiguration.BuildRadius;

        public bool ShouldRelease(float distanceSquared) =>
            distanceSquared > _flightFieldConfiguration.ReleaseRadius * _flightFieldConfiguration.ReleaseRadius;
    }
}
