namespace Features.FlightFieldModule.Scripts.Generation.Noise {
    // Smooth value noise from integer hashing only: the same seed and point give the same value on every machine,
    // and it touches no Unity object, so it runs on the generation thread. Two octaves, values in 0..1.
    public sealed class FieldValueNoise : IFieldNoise {
        private const uint SEED_PRIME = 0x9E3779B1u;
        private const uint X_PRIME = 0x85EBCA77u;
        private const uint Y_PRIME = 0xC2B2AE3Du;
        private const uint FIRST_MIX = 0x85EBCA6Bu;
        private const uint SECOND_MIX = 0xC2B2AE35u;
        private const int FIRST_SHIFT = 16;
        private const int SECOND_SHIFT = 13;
        private const float UINT_UNIT = 1f / uint.MaxValue;
        private const float FIRST_OCTAVE_WEIGHT = 0.65f;
        private const float SECOND_OCTAVE_WEIGHT = 0.35f;
        private const float SECOND_OCTAVE_FREQUENCY = 2.03f;
        // Keeps the second octave's lattice apart from the first one's.
        private const int SECOND_OCTAVE_SALT = 0x2545F491;

        public float Sample(int seed, float x, float y) =>
            Octave(seed, x, y) * FIRST_OCTAVE_WEIGHT
            + Octave(unchecked(seed + SECOND_OCTAVE_SALT), x * SECOND_OCTAVE_FREQUENCY, y * SECOND_OCTAVE_FREQUENCY) * SECOND_OCTAVE_WEIGHT;

        private static float Octave(int seed, float x, float y) {
            int cellX = FloorToInt(x);
            int cellY = FloorToInt(y);
            float u = Smooth(x - cellX);
            float v = Smooth(y - cellY);
            float bottom = Lerp(Hash(seed, cellX, cellY), Hash(seed, cellX + 1, cellY), u);
            float top = Lerp(Hash(seed, cellX, cellY + 1), Hash(seed, cellX + 1, cellY + 1), u);
            return Lerp(bottom, top, v);
        }

        private static float Hash(int seed, int x, int y) {
            unchecked {
                uint hash = (uint)seed * SEED_PRIME ^ (uint)x * X_PRIME ^ (uint)y * Y_PRIME;
                hash ^= hash >> FIRST_SHIFT;
                hash *= FIRST_MIX;
                hash ^= hash >> SECOND_SHIFT;
                hash *= SECOND_MIX;
                hash ^= hash >> FIRST_SHIFT;
                return hash * UINT_UNIT;
            }
        }

        private static int FloorToInt(float value) {
            int truncated = (int)value;
            return value < truncated ? truncated - 1 : truncated;
        }

        private static float Smooth(float t) =>
            t * t * (3f - 2f * t);

        private static float Lerp(float from, float to, float t) =>
            from + (to - from) * t;
    }
}
