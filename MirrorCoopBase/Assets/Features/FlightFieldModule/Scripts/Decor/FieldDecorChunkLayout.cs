using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // The volume rocks of one decor chunk, in field space (x across the field, z along it, y up from the flight
    // plane): the grid points of every band whose middle lies in the chunk, on both sides of the plane. A point that
    // comes closer than the band's gap to a neighbour with a higher priority gives way (Matern thinning), so two
    // rocks never fuse; every point and its neighbours come from the field seed and their grid coordinates only, so
    // every peer and a rejoining player see the same rocks whatever order the chunks are built in.
    public sealed class FieldDecorChunkLayout : IFieldDecorChunkLayout {
        private const float HALF = 0.5f;
        private const float UP = 1f;
        private const float DOWN = -1f;
        // The farthest vertex of a rock prefab from its pivot, as a share of the rock's size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;
        private static readonly int[] _sides = { (int)UP, (int)DOWN };

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldVolumeCandidates _fieldVolumeCandidates;
        private readonly IFieldVolumeSourceReader _fieldVolumeSourceReader;

        public FieldDecorChunkLayout(FlightFieldDecorConfiguration flightFieldDecorConfiguration, FlightFieldConfiguration flightFieldConfiguration,
                                     IFieldVolumeCandidates fieldVolumeCandidates, IFieldVolumeSourceReader fieldVolumeSourceReader) {
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldVolumeCandidates = fieldVolumeCandidates;
            _fieldVolumeSourceReader = fieldVolumeSourceReader;
        }

        public void Fill(int fieldSeed, Vector2Int chunk, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            rocks.Clear();
            if (!_fieldVolumeSourceReader.TryRead(out FieldVolumeSource source))
                return;

            IReadOnlyList<FieldVolumeConfiguration.Band> bands = _flightFieldConfiguration.Volume.Bands;
            for (int bandIndex = 0; bandIndex < bands.Count; bandIndex++) {
                foreach (int side in _sides)
                    FillBand(new FieldVolumeBandKey(bands[bandIndex], bandIndex, side), fieldSeed, chunk, source, random, rocks);
            }
        }

        private void FillBand(FieldVolumeBandKey key, int fieldSeed, Vector2Int chunk, FieldVolumeSource source, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            float spacing = key.Band.SpacingMeters;
            float chunkSize = _flightFieldDecorConfiguration.ChunkSize;
            Vector2Int first = new(FirstPointOf(chunk.x, chunkSize, spacing), FirstPointOf(chunk.y, chunkSize, spacing));
            Vector2Int last = new(FirstPointOf(chunk.x + 1, chunkSize, spacing) - 1, FirstPointOf(chunk.y + 1, chunkSize, spacing) - 1);
            int layers = _fieldVolumeCandidates.CountLayers(key.Band);
            for (int x = first.x; x <= last.x; x++) {
                for (int z = first.y; z <= last.y; z++)
                    FillColumn(key, new Vector2Int(x, z), layers, fieldSeed, source, random, rocks);
            }
        }

        private void FillColumn(FieldVolumeBandKey key, Vector2Int column, int layers, int fieldSeed, FieldVolumeSource source, IFieldRandom random,
                                List<FieldClusterRockPose> rocks) {
            for (int layer = 0; layer < layers; layer++) {
                Vector3Int point = new(column.x, layer, column.y);
                FieldVolumeCandidate candidate = _fieldVolumeCandidates.Get(key.Band, key.Index, key.Side, point, fieldSeed, source);
                if (candidate.Exists && !IsBeaten(key, point, candidate, layers, fieldSeed, source))
                    rocks.Add(CreateRock(key.Band, candidate, random));
            }
        }

        // A neighbour with a higher priority that stands closer than the gap wins, whether or not it gave way itself.
        private bool IsBeaten(FieldVolumeBandKey key, Vector3Int point, FieldVolumeCandidate candidate, int layers, int fieldSeed, FieldVolumeSource source) {
            FieldVolumeConfiguration.Band band = key.Band;
            float conflict = band.DiameterMeters.y + band.GapMeters;
            int reach = Mathf.CeilToInt(conflict / band.SpacingMeters) + 1;
            int layerReach = Mathf.CeilToInt(conflict * layers / (band.HeightMeters.y - band.HeightMeters.x)) + 1;
            for (int x = point.x - reach; x <= point.x + reach; x++) {
                for (int z = point.z - reach; z <= point.z + reach; z++) {
                    if (IsBeatenInColumn(key, new Vector3Int(x, point.y, z), point, candidate, layerReach, layers, fieldSeed, source))
                        return true;
                }
            }

            return false;
        }

        private bool IsBeatenInColumn(FieldVolumeBandKey key, Vector3Int column, Vector3Int point, FieldVolumeCandidate candidate, int layerReach, int layers, int fieldSeed,
                                      FieldVolumeSource source) {
            for (int layer = Mathf.Max(0, point.y - layerReach); layer <= Mathf.Min(layers - 1, point.y + layerReach); layer++) {
                Vector3Int other = new(column.x, layer, column.z);
                if (other == point)
                    continue;

                FieldVolumeCandidate neighbour = _fieldVolumeCandidates.Get(key.Band, key.Index, key.Side, other, fieldSeed, source);
                if (neighbour.Exists && Beats(neighbour, candidate) && IsTooClose(neighbour, candidate, key.Band.GapMeters))
                    return true;
            }

            return false;
        }

        private FieldClusterRockPose CreateRock(FieldVolumeConfiguration.Band band, FieldVolumeCandidate candidate, IFieldRandom random) {
            FieldVolumeConfiguration volume = _flightFieldConfiguration.Volume;
            random.Reseed(candidate.LookSeed);
            FieldRockVisual prefab = FieldClusterLayout.PickRock(volume.Rocks, random);
            Material material = band.Materials[random.Range(0, band.Materials.Count)];
            float size = candidate.Radius / (ROCK_REACH * volume.AxisScale.y);
            Vector3 scale = new(AxisScale(volume, random) * size, AxisScale(volume, random) * size, AxisScale(volume, random) * size);
            return new FieldClusterRockPose(prefab, material, candidate.Position, FieldClusterLayout.RandomRotation(random), scale);
        }

        // The first grid point whose middle lies at or past the chunk edge.
        private static int FirstPointOf(int chunk, float chunkSize, float spacing) =>
            Mathf.CeilToInt(chunk * chunkSize / spacing - HALF);

        private static bool Beats(FieldVolumeCandidate first, FieldVolumeCandidate second) =>
            first.Priority > second.Priority || Mathf.Approximately(first.Priority, second.Priority) && first.LookSeed > second.LookSeed;

        private static bool IsTooClose(FieldVolumeCandidate first, FieldVolumeCandidate second, float gap) =>
            Vector3.Distance(first.Position, second.Position) < first.Radius + second.Radius + gap;

        private static float AxisScale(FieldVolumeConfiguration volume, IFieldRandom random) =>
            Mathf.Lerp(volume.AxisScale.x, volume.AxisScale.y, random.Value());
    }
}
