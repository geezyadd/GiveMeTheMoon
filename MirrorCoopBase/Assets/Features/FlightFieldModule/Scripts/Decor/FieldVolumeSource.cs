using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // What the volume of a field is laid out from, the same on every peer: the profile and difficulty its ridges come
    // from, the volume configuration of that profile, the ends of the main route on the field plane (field space: x
    // across, y along), where the ship climbs from and sinks to the stations, and the giants it keeps clear of.
    public readonly struct FieldVolumeSource {
        public FieldDifficultyProfile Profile { get; }
        public FieldVolumeConfiguration Volume { get; }
        public float Difficulty { get; }
        public float CellSize { get; }
        public Vector2 RouteStart { get; }
        public Vector2 RouteEnd { get; }
        public IReadOnlyList<FieldVolumeGiant> Giants { get; }

        public FieldVolumeSource(FieldDifficultyProfile profile, FieldVolumeConfiguration volume, float difficulty, float cellSize, Vector2 routeStart, Vector2 routeEnd,
                                 IReadOnlyList<FieldVolumeGiant> giants) {
            Profile = profile;
            Volume = volume;
            Difficulty = difficulty;
            CellSize = cellSize;
            RouteStart = routeStart;
            RouteEnd = routeEnd;
            Giants = giants;
        }
    }
}
