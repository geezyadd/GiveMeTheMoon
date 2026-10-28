using System;
using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Packs a piece of ore's id into a weapon target id: the top byte marks ore (field rocks keep 0xF1, the debug
    // dummy 0xDE), the low 32 bits hold the piece's id.
    public sealed class FieldOreWeaponIds : IFieldOreWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong ORE_TAG = 0xF3UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong ID_MASK = 0xFFFFFFFFUL;
        private const string OUT_OF_RANGE_FORMAT = "Ore {0} does not fit a weapon target id.";

        public ulong GetTargetId(FieldOreId id) {
            if (id.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id.Value, string.Format(OUT_OF_RANGE_FORMAT, id));

            return ORE_TAG | (uint)id.Value;
        }

        public bool TryGetOreId(ulong targetId, out FieldOreId id) {
            id = default;
            if ((targetId & TAG_MASK) != ORE_TAG)
                return false;

            id = new FieldOreId((int)(targetId & ID_MASK));
            return true;
        }
    }
}
