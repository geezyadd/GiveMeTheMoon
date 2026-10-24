using System;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // A rock of the current flight field, the same on every peer: a cluster rock is the rock index inside the
    // cluster of a placement (by its index in the field's placement list), a debris rock the rock index inside its
    // debris cell. It names a rock in field space, so it stays the same across world shifts.
    public readonly struct FieldRockId : IEquatable<FieldRockId> {
        private const int NO_PLACEMENT = -1;

        public FieldRockSource Source { get; }
        public int PlacementIndex { get; }
        public Vector2Int DebrisCell { get; }
        public int RockIndex { get; }

        public FieldRockId(FieldRockSource source, int placementIndex, Vector2Int debrisCell, int rockIndex) {
            Source = source;
            PlacementIndex = placementIndex;
            DebrisCell = debrisCell;
            RockIndex = rockIndex;
        }

        public static FieldRockId CreateCluster(int placementIndex, int rockIndex) =>
            new(FieldRockSource.Cluster, placementIndex, Vector2Int.zero, rockIndex);

        public static FieldRockId CreateDebris(Vector2Int debrisCell, int rockIndex) =>
            new(FieldRockSource.Debris, NO_PLACEMENT, debrisCell, rockIndex);

        public bool Equals(FieldRockId other) =>
            Source == other.Source && PlacementIndex == other.PlacementIndex && DebrisCell == other.DebrisCell && RockIndex == other.RockIndex;

        public override bool Equals(object obj) =>
            obj is FieldRockId other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Source, PlacementIndex, DebrisCell, RockIndex);

        public override string ToString() =>
            Source == FieldRockSource.Cluster ? $"cluster {PlacementIndex}/{RockIndex}" : $"{Source} {DebrisCell}/{RockIndex}";
    }
}
