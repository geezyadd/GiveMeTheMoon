using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldDebrisPicker {
        // The id of a debris kind of the profile picked by weight.
        public string Pick(FieldDifficultyProfile profile, IFieldRandom random);
    }
}
