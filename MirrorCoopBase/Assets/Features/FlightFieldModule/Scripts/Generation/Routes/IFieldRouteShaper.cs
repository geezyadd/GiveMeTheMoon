using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // The replaceable part of route generation: profile and difficulty -> corridor width, winding and debris.
    public interface IFieldRouteShaper {
        public FieldCorridorShape Shape(FieldDifficultyProfile profile, float difficulty);
    }
}
