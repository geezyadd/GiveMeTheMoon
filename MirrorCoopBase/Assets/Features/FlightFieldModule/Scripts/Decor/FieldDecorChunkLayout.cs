using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // The volume rocks of one decor chunk, in field space (x across the field, z along it, y up from the flight
    // plane): the grid points of every band of the field's volume whose middle lies in the chunk, on both sides of the
    // plane. A point that comes closer than the band's gap to a neighbour with a higher priority gives way (Matern
    // thinning), and so does a point that comes too close to any rock of a later band (the bands go from small rocks
    // to big ones, so a small rock never sticks into a big one); no rock comes into a giant of an obstacle field. So
    // two rocks never fuse; every point and its neighbours come from the field seed and their grid coordinates only,
    // so every peer and a rejoining player see the same rocks whatever order the chunks are built in.
    public sealed class FieldDecorChunkLayout : IFieldDecorChunkLayout {
        private const float HALF = 0.5f;
        private const float UP = 1f;
        private const float DOWN = -1f;
        private static readonly int[] _sides = { (int)UP, (int)DOWN };

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        private readonly IFieldVolumeCandidates _fieldVolumeCandidates;
        private readonly IFieldVolumeSourceReader _fieldVolumeSourceReader;
        // The giants that can reach into the chunk being laid out: a field has up to a few hundred.
        private readonly List<FieldVolumeGiant> _chunkGiants = new();

        public FieldDecorChunkLayout(FlightFieldDecorConfiguration flightFieldDecorConfiguration, IFieldVolumeCandidates fieldVolumeCandidates,
                                     IFieldVolumeSourceReader fieldVolumeSourceReader) {
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
            _fieldVolumeCandidates = fieldVolumeCandidates;
            _fieldVolumeSourceReader = fieldVolumeSourceReader;
        }

        public void Fill(int fieldSeed, Vector2Int chunk, IFieldRandom random, List<FieldClusterRockPose> rocks) {
            rocks.Clear();
            if (!_fieldVolumeSourceReader.TryRead(out FieldVolumeSource source))
                return;

            CollectChunkGiants(chunk, source);
            IReadOnlyList<FieldVolumeConfiguration.Band> bands = source.Volume.Bands;
            for (int bandIndex = 0; bandIndex < bands.Count; bandIndex++) {
                foreach (int side in _sides)
                    FillBand(new FieldVolumeBandKey(bands[bandIndex], bandIndex, side), fieldSeed, chunk, source, random, rocks);
            }
        }

        private void CollectChunkGiants(Vector2Int chunk, FieldVolumeSource source) {
            _chunkGiants.Clear();
            float chunkSize = _flightFieldDecorConfiguration.ChunkSize;
            Vector2 min = (Vector2)chunk * chunkSize;
            float reach = LargestReach(source.Volume);
            foreach (FieldVolumeGiant giant in source.Giants) {
                Vector2 nearest = new(Mathf.Clamp(giant.Center.x, min.x, min.x + chunkSize), Mathf.Clamp(giant.Center.y, min.y, min.y + chunkSize));
                if (Vector2.Distance(nearest, giant.Center) < giant.Radius + reach)
                    _chunkGiants.Add(giant);
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
                if (!candidate.Exists || IsBeaten(key, point, candidate, layers, fieldSeed, source))
                    continue;

                if (!IsBeatenByLaterBand(key, candidate, fieldSeed, source) && !TouchesGiant(candidate, key.Band.GapMeters))
                    rocks.Add(CreateRock(source.Volume, key.Band, candidate, random));
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

        // Any rock of a later band on the same side wins, with the wider of the two gaps.
        private bool IsBeatenByLaterBand(FieldVolumeBandKey key, FieldVolumeCandidate candidate, int fieldSeed, FieldVolumeSource source) {
            IReadOnlyList<FieldVolumeConfiguration.Band> bands = source.Volume.Bands;
            for (int index = key.Index + 1; index < bands.Count; index++) {
                float gap = Mathf.Max(key.Band.GapMeters, bands[index].GapMeters);
                if (IsBeatenByBand(new FieldVolumeBandKey(bands[index], index, key.Side), candidate, gap, fieldSeed, source))
                    return true;
            }

            return false;
        }

        private bool IsBeatenByBand(FieldVolumeBandKey other, FieldVolumeCandidate candidate, float gap, int fieldSeed, FieldVolumeSource source) {
            float spacing = other.Band.SpacingMeters;
            float conflict = candidate.Radius + other.Band.DiameterMeters.y * HALF + gap;
            Vector2Int first = new(Mathf.FloorToInt((candidate.Position.x - conflict) / spacing), Mathf.FloorToInt((candidate.Position.z - conflict) / spacing));
            Vector2Int last = new(Mathf.FloorToInt((candidate.Position.x + conflict) / spacing), Mathf.FloorToInt((candidate.Position.z + conflict) / spacing));
            int layers = _fieldVolumeCandidates.CountLayers(other.Band);
            for (int x = first.x; x <= last.x; x++) {
                for (int z = first.y; z <= last.y; z++) {
                    if (IsBeatenInOtherColumn(other, new Vector2Int(x, z), layers, candidate, gap, fieldSeed, source))
                        return true;
                }
            }

            return false;
        }

        private bool IsBeatenInOtherColumn(FieldVolumeBandKey other, Vector2Int column, int layers, FieldVolumeCandidate candidate, float gap, int fieldSeed,
                                           FieldVolumeSource source) {
            for (int layer = 0; layer < layers; layer++) {
                FieldVolumeCandidate neighbour = _fieldVolumeCandidates.Get(other.Band, other.Index, other.Side, new Vector3Int(column.x, layer, column.y), fieldSeed, source);
                if (neighbour.Exists && IsTooClose(neighbour, candidate, gap))
                    return true;
            }

            return false;
        }

        private bool TouchesGiant(FieldVolumeCandidate candidate, float gap) {
            foreach (FieldVolumeGiant giant in _chunkGiants) {
                if (Vector3.Distance(candidate.Position, new Vector3(giant.Center.x, 0f, giant.Center.y)) < giant.Radius + candidate.Radius + gap)
                    return true;
            }

            return false;
        }

        private static FieldClusterRockPose CreateRock(FieldVolumeConfiguration volume, FieldVolumeConfiguration.Band band, FieldVolumeCandidate candidate, IFieldRandom random) {
            random.Reseed(candidate.LookSeed);
            FieldRockVisual prefab = FieldClusterLayout.PickRock(volume.Rocks, random);
            Material material = band.Materials[random.Range(0, band.Materials.Count)];
            float size = candidate.Radius / (FieldRockVisual.MESH_REACH * volume.AxisScale.y);
            Vector3 scale = new(AxisScale(volume, random) * size, AxisScale(volume, random) * size, AxisScale(volume, random) * size);
            return new FieldClusterRockPose(prefab, material, candidate.Position, FieldClusterLayout.RandomRotation(random), scale);
        }

        // The farthest a rock of the volume reaches from its middle, with its gap.
        private static float LargestReach(FieldVolumeConfiguration volume) {
            float reach = 0f;
            foreach (FieldVolumeConfiguration.Band band in volume.Bands)
                reach = Mathf.Max(reach, band.DiameterMeters.y * HALF + band.GapMeters);

            return reach;
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
