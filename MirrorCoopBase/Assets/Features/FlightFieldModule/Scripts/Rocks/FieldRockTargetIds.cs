using System;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Packs a rock id into a weapon target id: the top byte marks a field rock (other targets keep their own top byte,
    // the debug dummy 0xDE), the next four bits the source, the low 48 bits a cluster's placement and rock index or a
    // debris cell and rock index, 16 bits each but the placement's 32.
    public sealed class FieldRockTargetIds : IFieldRockTargetIds {
        private const ulong ROCK_TAG = 0xF1UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const int TAG_SHIFT = 56;
        private const int SOURCE_SHIFT = 52;
        private const ulong SOURCE_MASK = 0xFUL;
        private const int HIGH_SHIFT = 32;
        private const int MIDDLE_SHIFT = 16;
        private const ulong FIELD_MASK = 0xFFFFUL;
        private const ulong PLACEMENT_MASK = 0xFFFFFFFFUL;
        private const string OUT_OF_RANGE_FORMAT = "Rock {0} does not fit a weapon target id.";

        public ulong GetTargetId(FieldRockId id) {
            ulong payload = id.Source switch {
                FieldRockSource.Cluster => PackCluster(id),
                FieldRockSource.Debris => PackDebris(id),
                _ => throw new ArgumentOutOfRangeException(nameof(id), id.Source, $"Rock {id} has no source."),
            };
            return ROCK_TAG | ((ulong)id.Source << SOURCE_SHIFT) | payload;
        }

        public bool TryGetRockId(ulong targetId, out FieldRockId id) {
            id = default;
            if ((targetId & TAG_MASK) != ROCK_TAG)
                return false;

            int rockIndex = (int)(targetId & FIELD_MASK);
            switch ((FieldRockSource)((targetId >> SOURCE_SHIFT) & SOURCE_MASK)) {
                case FieldRockSource.Cluster:
                    id = FieldRockId.CreateCluster((int)((targetId >> MIDDLE_SHIFT) & PLACEMENT_MASK), rockIndex);
                    return true;
                case FieldRockSource.Debris:
                    id = FieldRockId.CreateDebris(new Vector2Int((int)((targetId >> HIGH_SHIFT) & FIELD_MASK), (int)((targetId >> MIDDLE_SHIFT) & FIELD_MASK)), rockIndex);
                    return true;
                default:
                    return false;
            }
        }

        private static ulong PackCluster(FieldRockId id) {
            if (id.PlacementIndex < 0 || !FitsField(id.RockIndex))
                throw new ArgumentOutOfRangeException(nameof(id), string.Format(OUT_OF_RANGE_FORMAT, id));

            return ((ulong)id.PlacementIndex << MIDDLE_SHIFT) | (ulong)id.RockIndex;
        }

        private static ulong PackDebris(FieldRockId id) {
            if (!FitsField(id.DebrisCell.x) || !FitsField(id.DebrisCell.y) || !FitsField(id.RockIndex))
                throw new ArgumentOutOfRangeException(nameof(id), string.Format(OUT_OF_RANGE_FORMAT, id));

            return ((ulong)id.DebrisCell.x << HIGH_SHIFT) | ((ulong)id.DebrisCell.y << MIDDLE_SHIFT) | (ulong)id.RockIndex;
        }

        private static bool FitsField(int value) =>
            value >= 0 && (ulong)value <= FIELD_MASK;
    }
}
