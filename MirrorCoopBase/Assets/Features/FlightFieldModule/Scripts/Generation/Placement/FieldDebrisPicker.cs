using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Picks one debris kind of a profile with a chance proportional to its weight.
    public sealed class FieldDebrisPicker : IFieldDebrisPicker {
        public string Pick(FieldDifficultyProfile profile, IFieldRandom random) {
            IReadOnlyList<FieldDebrisOption> options = profile.DebrisTypes;
            float total = 0f;
            foreach (FieldDebrisOption option in options)
                total += option.Weight;

            if (total <= 0f)
                throw new InvalidOperationException($"Profile {profile.Id} has no debris kind with a positive weight.");

            float roll = random.Value() * total;
            foreach (FieldDebrisOption option in options) {
                roll -= option.Weight;
                if (roll < 0f)
                    return option.Debris.Id;
            }

            return options[options.Count - 1].Debris.Id;
        }
    }
}
