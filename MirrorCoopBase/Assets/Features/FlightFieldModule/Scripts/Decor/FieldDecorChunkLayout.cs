using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Where the decor rocks of one chunk stand, in field space (x across the field, z along it, y up from the flight
    // plane). The sequence is reseeded from the field seed and the chunk coordinates, so every peer and a rejoining
    // player see the same rocks whatever order the chunks are built in.
    public sealed class FieldDecorChunkLayout : IFieldDecorChunkLayout {
        private const float SQUARE_METERS_PER_SQUARE_KILOMETER = 1000000f;
        // Large primes keep the sequences of neighbouring chunks apart and away from the structure sequences.
        private const int CHUNK_X_SEED_STEP = 73856093;
        private const int CHUNK_Z_SEED_STEP = 83492791;
        private const int DECOR_SEED_SALT = 19349663;
        private const float UP = 1f;
        private const float DOWN = -1f;

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;

        public FieldDecorChunkLayout(FlightFieldDecorConfiguration flightFieldDecorConfiguration) =>
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;

        public void Fill(int fieldSeed, Vector2Int chunk, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            rocks.Clear();
            random.Reseed(CreateChunkSeed(fieldSeed, chunk));
            FillLayer(chunk, UP, random, rocks);
            FillLayer(chunk, DOWN, random, rocks);
        }

        private static int CreateChunkSeed(int fieldSeed, Vector2Int chunk) =>
            unchecked(fieldSeed + DECOR_SEED_SALT + chunk.x * CHUNK_X_SEED_STEP + chunk.y * CHUNK_Z_SEED_STEP);

        private void FillLayer(Vector2Int chunk, float side, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            float chunkSize = _flightFieldDecorConfiguration.ChunkSize;
            float expected = _flightFieldDecorConfiguration.RocksPerSquareKilometer * chunkSize * chunkSize / SQUARE_METERS_PER_SQUARE_KILOMETER;
            int count = Mathf.FloorToInt(expected);
            if (random.Value() < expected - count)
                count++;

            for (int i = 0; i < count; i++)
                rocks.Add(CreateRock(chunk, chunkSize, side, random));
        }

        private FieldClusterRockPose CreateRock(Vector2Int chunk, float chunkSize, float side, IFieldRandom random) {
            Vector2 layerHeight = _flightFieldDecorConfiguration.LayerHeight;
            float heightShare = random.Value();
            float height = Mathf.Lerp(layerHeight.x, layerHeight.y, heightShare);
            Vector3 position = new((chunk.x + random.Value()) * chunkSize, side * height, (chunk.y + random.Value()) * chunkSize);
            Vector2 sizeRange = _flightFieldDecorConfiguration.RockSize;
            float size = Mathf.Lerp(sizeRange.x, sizeRange.y, random.Value());
            Vector3 scale = new(AxisScale(random) * size, AxisScale(random) * size, AxisScale(random) * size);
            return new FieldClusterRockPose(FieldClusterLayout.PickPrefab(_flightFieldDecorConfiguration.RockRecipe, random), PickMaterial(heightShare), position,
                                            FieldClusterLayout.RandomRotation(random), scale);
        }

        private float AxisScale(IFieldRandom random) {
            Vector2 axisScale = _flightFieldDecorConfiguration.RockRecipe.AxisScale;
            return Mathf.Lerp(axisScale.x, axisScale.y, random.Value());
        }

        // The farther from the flight plane, the darker the material.
        private Material PickMaterial(float heightShare) {
            IReadOnlyList<Material> materials = _flightFieldDecorConfiguration.MaterialsByDistance;
            return materials[Mathf.Min(Mathf.FloorToInt(heightShare * materials.Count), materials.Count - 1)];
        }
    }
}
