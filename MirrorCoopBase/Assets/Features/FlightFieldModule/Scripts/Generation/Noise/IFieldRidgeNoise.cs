using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Noise {
    public interface IFieldRidgeNoise {
        // The ridge part of the density map at a field position in cells (x along the field, y across it), 0..1:
        // the profile's low-frequency noise sharpened by its contrast. Every peer gets the same value from the seed.
        public float Sample(FieldDifficultyProfile profile, float difficulty, int fieldSeed, Vector2 cell);
    }
}
