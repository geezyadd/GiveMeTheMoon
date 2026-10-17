using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Picks one debris kind of a profile with a chance proportional to its weight.
    public sealed class FieldDebrisPicker : IFieldDebrisPicker {
        public string Pick(FieldDifficultyProfile profile, IFieldRandom random) =>
            FieldWeightedPick.Pick(profile.DebrisTypes, random).Debris.Id;
    }
}
