using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Density map: low-frequency noise sharpened by the profile's contrast, plus a few large voids. The obstacle
    // density of the giants and the debris follows it.
    public sealed class FieldDensityMapStep : IFieldGenerationStep {
        private readonly IFieldRidgeNoise _fieldRidgeNoise;

        public FieldDensityMapStep(IFieldRidgeNoise fieldRidgeNoise) =>
            _fieldRidgeNoise = fieldRidgeNoise;

        public void Execute(FieldGenerationContext context) {
            Vector2Int size = context.Frame.Size;
            FieldDensityMap map = new(size);
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    Vector2Int cell = new(x, y);
                    map.SetDensity(cell, Mathf.Clamp01(_fieldRidgeNoise.Sample(context.Profile, context.Difficulty, context.Seed, cell)));
                }
            }

            AddVoids(context, map);
            context.Density = map;
        }

        private static void AddVoids(FieldGenerationContext context, FieldDensityMap map) {
            FieldDifficultyProfile profile = context.Profile;
            int count = Mathf.RoundToInt(profile.VoidCount.Evaluate(context.Difficulty));
            Vector2Int size = context.Frame.Size;
            for (int i = 0; i < count; i++) {
                Vector2Int center = new(context.Random.Range(0, size.x), context.Random.Range(0, size.y));
                float radius = Mathf.Lerp(profile.VoidRadiusCells.x, profile.VoidRadiusCells.y, context.Random.Value());
                MarkVoid(map, center, radius, size);
            }
        }

        private static void MarkVoid(FieldDensityMap map, Vector2Int center, float radius, Vector2Int size) {
            int reach = Mathf.CeilToInt(radius);
            for (int y = Mathf.Max(0, center.y - reach); y <= Mathf.Min(size.y - 1, center.y + reach); y++) {
                for (int x = Mathf.Max(0, center.x - reach); x <= Mathf.Min(size.x - 1, center.x + reach); x++) {
                    Vector2Int cell = new(x, y);
                    if ((cell - center).sqrMagnitude <= radius * radius)
                        map.MarkVoid(cell);
                }
            }
        }
    }
}
