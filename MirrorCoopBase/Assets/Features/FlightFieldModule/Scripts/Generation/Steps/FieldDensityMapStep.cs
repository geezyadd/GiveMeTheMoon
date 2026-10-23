using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Density map: low-frequency noise sharpened by the profile's contrast (ridges and gaps), raised beside every
    // corridor (the shoulder of rocks that makes a corridor readable), plus the glade layer (noise below the glade
    // threshold) and a few large voids. The cell classes are decided from it later.
    public sealed class FieldDensityMapStep : IFieldGenerationStep {
        private const int GLADE_SALT = 0x3C6EF372;
        private static readonly Vector2Int[] _neighbourSteps = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        private readonly IFieldNoise _fieldNoise;
        private readonly IFieldRidgeNoise _fieldRidgeNoise;

        public FieldDensityMapStep(IFieldNoise fieldNoise, IFieldRidgeNoise fieldRidgeNoise) {
            _fieldNoise = fieldNoise;
            _fieldRidgeNoise = fieldRidgeNoise;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            true;

        public void Execute(FieldGenerationContext context) {
            Vector2Int size = context.Frame.Size;
            FieldDensityMap map = new(size);
            int[,] corridorDistance = MeasureCorridorDistance(context, Mathf.CeilToInt(context.Profile.ShoulderWidthCells.Evaluate(context.Difficulty)));
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++)
                    FillCell(context, map, new Vector2Int(x, y), corridorDistance[x, y]);
            }

            AddVoids(context, map);
            context.Density = map;
        }

        private void FillCell(FieldGenerationContext context, FieldDensityMap map, Vector2Int cell, int corridorDistance) {
            FieldDifficultyProfile profile = context.Profile;
            float difficulty = context.Difficulty;
            float density = _fieldRidgeNoise.Sample(profile, difficulty, context.Seed, cell) + Shoulder(profile, difficulty, corridorDistance);
            map.SetDensity(cell, Mathf.Clamp01(density));
            float glade = _fieldNoise.Sample(unchecked(context.Seed + GLADE_SALT), cell.x / profile.GladeNoiseScaleCells, cell.y / profile.GladeNoiseScaleCells);
            if (glade < profile.GladeThreshold.Evaluate(difficulty))
                map.MarkGlade(cell);
        }

        // The shoulder is strongest right beside the corridor and fades to nothing at its width.
        private static float Shoulder(FieldDifficultyProfile profile, float difficulty, int corridorDistance) {
            float width = profile.ShoulderWidthCells.Evaluate(difficulty);
            bool isBeside = corridorDistance > 0 && corridorDistance <= width;
            return isBeside ? profile.ShoulderStrength.Evaluate(difficulty) * (1f - (corridorDistance - 1) / Mathf.Max(1f, width)) : 0f;
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

        // Steps (diagonals count as one) from every cell to the nearest corridor cell, up to the limit; 0 inside a
        // corridor, int.MaxValue beyond the limit.
        private static int[,] MeasureCorridorDistance(FieldGenerationContext context, int limit) {
            Vector2Int size = context.Frame.Size;
            int[,] distance = new int[size.x, size.y];
            Queue<Vector2Int> open = new();
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    bool isCorridor = context.Cells[x, y].IsCorridor;
                    distance[x, y] = isCorridor ? 0 : int.MaxValue;
                    if (isCorridor)
                        open.Enqueue(new Vector2Int(x, y));
                }
            }

            while (open.Count > 0)
                Spread(distance, open, open.Dequeue(), limit, size);

            return distance;
        }

        private static void Spread(int[,] distance, Queue<Vector2Int> open, Vector2Int cell, int limit, Vector2Int size) {
            int next = distance[cell.x, cell.y] + 1;
            if (next > limit)
                return;

            foreach (Vector2Int step in _neighbourSteps) {
                Vector2Int neighbour = cell + step;
                bool isCloser = neighbour.x >= 0 && neighbour.x < size.x && neighbour.y >= 0 && neighbour.y < size.y && distance[neighbour.x, neighbour.y] > next;
                if (!isCloser)
                    continue;

                distance[neighbour.x, neighbour.y] = next;
                open.Enqueue(neighbour);
            }
        }
    }
}
