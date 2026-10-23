using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Noise {
    // Ridges and gaps of a field: low-frequency noise at the profile's scale and stretch, pushed away from its middle
    // by the profile's contrast. The generation builds the density map on it, every peer the volume of rocks above
    // and below the plane, so both follow the same ridges.
    public sealed class FieldRidgeNoise : IFieldRidgeNoise {
        private const int DENSITY_SALT = 0x68E31DA4;
        private const float MIN_CONTRAST = 0.01f;
        private const float HALF = 0.5f;

        private readonly IFieldNoise _fieldNoise;

        public FieldRidgeNoise(IFieldNoise fieldNoise) =>
            _fieldNoise = fieldNoise;

        public float Sample(FieldDifficultyProfile profile, float difficulty, int fieldSeed, Vector2 cell) {
            float scale = profile.NoiseScaleCells.Evaluate(difficulty);
            float noise = _fieldNoise.Sample(unchecked(fieldSeed + DENSITY_SALT), cell.x / scale, cell.y / (scale * profile.NoiseStretchAcross));
            return Sharpen(noise, profile.Contrast.Evaluate(difficulty));
        }

        // A contrast of 1 keeps the noise, a higher one hardens ridges and gaps.
        private static float Sharpen(float value, float contrast) {
            float centered = value * 2f - 1f;
            float sharpened = Mathf.Sign(centered) * Mathf.Pow(Mathf.Abs(centered), 1f / Mathf.Max(MIN_CONTRAST, contrast));
            return (sharpened + 1f) * HALF;
        }
    }
}
