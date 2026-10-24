using System;
using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Packs a target balloon id into a weapon target id: the top byte marks a balloon (field rocks keep 0xF1, the debug
    // dummy 0xDE), the low 32 bits hold the balloon's index.
    public sealed class FieldTargetWeaponIds : IFieldTargetWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong BALLOON_TAG = 0xF2UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong INDEX_MASK = 0xFFFFFFFFUL;

        public ulong GetTargetId(FieldTargetId id) {
            if (id.Index < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id.Index, $"Balloon {id} does not fit a weapon target id.");

            return BALLOON_TAG | (uint)id.Index;
        }

        public bool TryGetBalloonId(ulong targetId, out FieldTargetId id) {
            id = default;
            if ((targetId & TAG_MASK) != BALLOON_TAG)
                return false;

            id = new FieldTargetId((int)(targetId & INDEX_MASK));
            return true;
        }
    }
}
