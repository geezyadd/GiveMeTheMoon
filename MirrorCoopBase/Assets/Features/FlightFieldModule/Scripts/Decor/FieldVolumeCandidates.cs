using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Draws the grid points of the volume: a point anywhere in its grid cell and layer, a rock with the band's
    // presence at the ridge noise below it (more over ridges, fewer over glades, never none), bigger over ridges, kept
    // inside its band and away from the flight plane, and none below the plane near the route's ends. The draws come
    // in a fixed order from a sequence seeded by the field seed and the point, so a point is the same whoever asks.
    public sealed class FieldVolumeCandidates : IFieldVolumeCandidates {
        private const float HALF = 0.5f;
        private const uint POINT_X_PRIME = 0x9E3779B1u;
        private const uint POINT_Y_PRIME = 0x85EBCA77u;
        private const uint POINT_Z_PRIME = 0xC2B2AE3Du;
        private const uint BAND_PRIME = 0x27D4EB2Fu;
        private const uint SIDE_PRIME = 0x165667B1u;
        private const uint LOOK_SALT = 0x5BD1E995u;
        // How much of a rock's size follows the ridge below it; the rest is chance.
        private const float RIDGE_SIZE_SHARE = 0.5f;

        private readonly IFieldRidgeNoise _fieldRidgeNoise;
        private readonly IFieldRandom _random;

        public FieldVolumeCandidates(IFieldRidgeNoise fieldRidgeNoise, IFieldRandomFactory fieldRandomFactory) {
            _fieldRidgeNoise = fieldRidgeNoise;
            _random = fieldRandomFactory.Create(0);
        }

        public int CountLayers(FieldVolumeConfiguration.Band band) =>
            Mathf.Max(1, Mathf.FloorToInt((band.HeightMeters.y - band.HeightMeters.x) / band.SpacingMeters));

        public FieldVolumeCandidate Get(FieldVolumeConfiguration.Band band, int bandIndex, int side, Vector3Int point, int fieldSeed, FieldVolumeSource source) {
            int seed = PointSeed(fieldSeed, bandIndex, side, point);
            _random.Reseed(seed);
            float spacing = band.SpacingMeters;
            float layerHeight = (band.HeightMeters.y - band.HeightMeters.x) / CountLayers(band);
            Vector2 onPlane = new((point.x + _random.Value()) * spacing, (point.z + _random.Value()) * spacing);
            float layerShare = _random.Value();
            float priority = _random.Value();
            float presenceRoll = _random.Value();
            float sizeRoll = _random.Value();
            float ridge = _fieldRidgeNoise.Sample(source.Profile, source.Difficulty, fieldSeed, new Vector2(onPlane.y / source.CellSize, onPlane.x / source.CellSize));
            float radius = Mathf.Lerp(band.DiameterMeters.x, band.DiameterMeters.y, Mathf.Lerp(sizeRoll, ridge, RIDGE_SIZE_SHARE)) * HALF;
            float lowest = Mathf.Max(band.HeightMeters.x, source.Volume.PlaneClearanceMeters) + radius + band.GapMeters * HALF;
            float highest = band.HeightMeters.y - radius - band.GapMeters * HALF;
            float height = Mathf.Clamp(band.HeightMeters.x + (point.y + layerShare) * layerHeight, lowest, highest);
            bool exists = presenceRoll < Mathf.Lerp(band.Presence.x, band.Presence.y, ridge) && lowest <= highest && !IsNearStation(side, onPlane, radius, source);
            int lookSeed = unchecked((int)((uint)seed ^ LOOK_SALT));
            return new FieldVolumeCandidate(new Vector3(onPlane.x, side * height, onPlane.y), radius, priority, exists, lookSeed);
        }

        // Below the plane near the route's ends the ship climbs from and sinks to the stations.
        private bool IsNearStation(int side, Vector2 onPlane, float radius, FieldVolumeSource source) {
            float reach = source.Volume.StationClearanceMeters + radius;
            bool isBelow = side < 0;
            return isBelow && (Vector2.Distance(onPlane, source.RouteStart) < reach || Vector2.Distance(onPlane, source.RouteEnd) < reach);
        }

        private static int PointSeed(int fieldSeed, int bandIndex, int side, Vector3Int point) {
            unchecked {
                uint hash = (uint)fieldSeed ^ (uint)point.x * POINT_X_PRIME ^ (uint)point.y * POINT_Y_PRIME ^ (uint)point.z * POINT_Z_PRIME
                    ^ (uint)bandIndex * BAND_PRIME ^ (uint)(side + 2) * SIDE_PRIME;
                return (int)hash;
            }
        }
    }
}
