using System;

namespace Features.FlightFieldModule.Scripts.Generation {
    // SplitMix64: the same seed gives the same sequence on every machine and Unity version, unlike UnityEngine.Random.
    public sealed class FieldRandom : IFieldRandom {
        private const ulong GOLDEN_GAMMA = 0x9E3779B97F4A7C15UL;
        private const ulong FIRST_MIX = 0xBF58476D1CE4E5B9UL;
        private const ulong SECOND_MIX = 0x94D049BB133111EBUL;
        private const int FIRST_SHIFT = 30;
        private const int SECOND_SHIFT = 27;
        private const int LAST_SHIFT = 31;
        private const int FLOAT_BITS = 24;
        private const int UNUSED_BITS = 64 - FLOAT_BITS;
        private const float FLOAT_UNIT = 1f / (1 << FLOAT_BITS);

        private ulong _state;

        public FieldRandom(int seed) =>
            _state = (uint)seed;

        public int Range(int minInclusive, int maxExclusive) {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, $"The range [{minInclusive}, {maxExclusive}) is empty.");

            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(Next() % span));
        }

        public float Value() =>
            (Next() >> UNUSED_BITS) * FLOAT_UNIT;

        private ulong Next() {
            unchecked {
                _state += GOLDEN_GAMMA;
                ulong mixed = _state;
                mixed = (mixed ^ (mixed >> FIRST_SHIFT)) * FIRST_MIX;
                mixed = (mixed ^ (mixed >> SECOND_SHIFT)) * SECOND_MIX;
                return mixed ^ (mixed >> LAST_SHIFT);
            }
        }
    }
}
