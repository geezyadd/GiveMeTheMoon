using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Obstacles {
    public interface IFieldObstacleDensity {
        // Chance that a debris grid point of the cell holds a rock: the profile's density moved by the density noise
        // and lowered by the edge ramp.
        public float GetDebrisDensity(FieldGenerationContext context, Vector2Int cell);
        // Chance that a giant drawn with its middle in the cell is kept there.
        public float GetGiantChance(FieldGenerationContext context, Vector2Int cell);
    }
}
