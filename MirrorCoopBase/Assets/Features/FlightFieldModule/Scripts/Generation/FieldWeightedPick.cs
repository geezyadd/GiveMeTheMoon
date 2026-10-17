using System;
using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Generation {
    // The one weighted roll of the module: an option is picked with a chance proportional to its weight, taking one
    // value of the sequence, so the same seed picks the same option on every peer.
    public static class FieldWeightedPick {
        public static T Pick<T>(IReadOnlyList<T> options, IFieldRandom random) where T : IFieldWeighted {
            float totalWeight = 0f;
            foreach (T option in options)
                totalWeight += option.Weight;

            if (totalWeight <= 0f)
                throw new ArgumentException($"No {typeof(T).Name} with a positive weight to pick from.", nameof(options));

            float roll = random.Value() * totalWeight;
            foreach (T option in options) {
                roll -= option.Weight;
                if (roll < 0f)
                    return option;
            }

            return LastWeighted(options);
        }

        // Float rounding can leave the roll at the total; the last option with a weight takes it.
        private static T LastWeighted<T>(IReadOnlyList<T> options) where T : IFieldWeighted {
            for (int i = options.Count - 1; i > 0; i--) {
                if (options[i].Weight > 0f)
                    return options[i];
            }

            return options[0];
        }
    }
}
