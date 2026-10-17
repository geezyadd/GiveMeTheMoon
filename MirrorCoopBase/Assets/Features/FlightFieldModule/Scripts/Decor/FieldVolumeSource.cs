using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // What the volume of a field is laid out from, the same on every peer: the profile and difficulty its ridges come
    // from and the ends of the main route on the field plane (field space: x across, y along), where the ship climbs
    // from and sinks to the stations.
    public readonly struct FieldVolumeSource {
        public FieldDifficultyProfile Profile { get; }
        public float Difficulty { get; }
        public float CellSize { get; }
        public Vector2 RouteStart { get; }
        public Vector2 RouteEnd { get; }

        public FieldVolumeSource(FieldDifficultyProfile profile, float difficulty, float cellSize, Vector2 routeStart, Vector2 routeEnd) {
            Profile = profile;
            Difficulty = difficulty;
            CellSize = cellSize;
            RouteStart = routeStart;
            RouteEnd = routeEnd;
        }
    }
}
