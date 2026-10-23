using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Obstacles {
    // The density of an obstacle field: the profile's density, moved either way by the low-frequency density map (so
    // rocks clump and thin out with no rows to see) and ramped down towards the border when the profile asks for it.
    // Giants follow the same map, only never above the full chance.
    public sealed class FieldObstacleDensity : IFieldObstacleDensity {
        public float GetDebrisDensity(FieldGenerationContext context, Vector2Int cell) {
            FieldDifficultyProfile profile = context.Profile;
            float variation = profile.ObstacleDensityVariation;
            float noise = Mathf.Lerp(1f - variation, 1f + variation, context.Density.GetDensity(cell));
            return Mathf.Clamp01(profile.ObstacleDensity.Evaluate(context.Difficulty) * noise * GetEdgeShare(profile, cell, context.Frame.Size));
        }

        public float GetGiantChance(FieldGenerationContext context, Vector2Int cell) {
            FieldDifficultyProfile profile = context.Profile;
            float noise = Mathf.Lerp(1f - profile.ObstacleDensityVariation, 1f, context.Density.GetDensity(cell));
            return noise * GetEdgeShare(profile, cell, context.Frame.Size);
        }

        // 1 inside the field, the profile's edge share right at the border when the ramp is on.
        private static float GetEdgeShare(FieldDifficultyProfile profile, Vector2Int cell, Vector2Int size) {
            if (profile.EdgeRampCells <= 0)
                return 1f;

            int toBorder = Mathf.Min(Mathf.Min(cell.x, size.x - 1 - cell.x), Mathf.Min(cell.y, size.y - 1 - cell.y));
            return Mathf.Lerp(profile.EdgeDensityShare, 1f, Mathf.Clamp01(toBorder / (float)profile.EdgeRampCells));
        }
    }
}
