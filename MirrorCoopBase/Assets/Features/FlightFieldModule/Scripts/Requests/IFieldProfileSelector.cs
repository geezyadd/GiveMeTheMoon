using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Requests {
    public interface IFieldProfileSelector {
        // One of the entry's profiles, by weight, from the field seed.
        public FieldDifficultyProfile Select(FlightFieldConfiguration.LoopRoutes loopRoutes, int seed);
    }
}
