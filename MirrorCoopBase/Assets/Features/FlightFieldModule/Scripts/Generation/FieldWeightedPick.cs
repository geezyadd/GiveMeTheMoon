using System;
using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Generation {
    // The one weighted roll of the module: an option is picked with a chance proportional to its weight, taking one
    // value of the sequence, so the same seed picks the same option on every peer.
    public static class FieldWeightedPick {
        public static T Pick<T>(IReadOnlyList<T> options, IFieldRandom random) where T : IFieldWeighted =>
            Pick(options, random.Value());

        // For a caller that drew its roll (0..1) with its other values in a fixed order.
        public static T Pick<T>(IReadOnlyList<T> options, float roll) where T : IFieldWeighted {
            float totalWeight = 0f;
            foreach (T option in options)
                totalWeight += option.Weight;

            if (totalWeight <= 0f)
                throw new ArgumentException($"No {typeof(T).Name} with a positive weight to pick from.", nameof(options));

            float left = roll * totalWeight;
            foreach (T option in options) {
                left -= option.Weight;
                if (left < 0f)
                    return option;
            }

            return LastWeighted(options);
        }

        // For many options picked many times: the running totals of their weights (each above the one before for a
        // positive weight) are made once, a pick is a binary search. Gives the index the plain pick would give.
        public static int PickIndex(IReadOnlyList<float> runningWeights, IFieldRandom random) {
            float total = runningWeights.Count == 0 ? 0f : runningWeights[runningWeights.Count - 1];
            if (total <= 0f)
                throw new ArgumentException("No option with a positive weight to pick from.", nameof(runningWeights));

            float left = random.Value() * total;
            int low = 0;
            int high = runningWeights.Count - 1;
            while (low < high) {
                int middle = (low + high) / 2;
                if (runningWeights[middle] > left)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low;
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
