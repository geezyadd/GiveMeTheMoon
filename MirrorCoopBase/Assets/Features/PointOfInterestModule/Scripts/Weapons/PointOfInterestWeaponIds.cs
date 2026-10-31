using System;

namespace Features.PointOfInterestModule.Scripts.Weapons {
    // Packs a point of interest's id into a weapon target id: the top byte marks a reward container (field rocks keep
    // 0xF1, ore 0xF3, bugs 0xB0, drifters 0xF4, the debug dummy 0xDE), the low 32 bits hold the point id.
    public sealed class PointOfInterestWeaponIds : IPointOfInterestWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong CONTAINER_TAG = 0xC5UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong ID_MASK = 0xFFFFFFFFUL;

        public ulong GetTargetId(int pointId) {
            if (pointId < 0)
                throw new ArgumentOutOfRangeException(nameof(pointId), pointId, $"Point of interest {pointId} does not fit a weapon target id.");

            return CONTAINER_TAG | (uint)pointId;
        }

        public bool TryGetPointId(ulong targetId, out int pointId) {
            pointId = 0;
            if ((targetId & TAG_MASK) != CONTAINER_TAG)
                return false;

            pointId = (int)(targetId & ID_MASK);
            return true;
        }
    }
}
