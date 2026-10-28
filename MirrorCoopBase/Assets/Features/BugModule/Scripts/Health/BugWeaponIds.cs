using System;

namespace Features.BugModule.Scripts.Health {
    // Packs a bug's field agent id into a weapon target id: the top byte marks a bug (field rocks keep 0xF1, balloons
    // 0xF2, the debug dummy 0xDE), the low 32 bits hold the agent id.
    public sealed class BugWeaponIds : IBugWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong BUG_TAG = 0xB0UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong ID_MASK = 0xFFFFFFFFUL;

        public ulong GetTargetId(int bugId) {
            if (bugId < 0)
                throw new ArgumentOutOfRangeException(nameof(bugId), bugId, $"Bug {bugId} does not fit a weapon target id.");

            return BUG_TAG | (uint)bugId;
        }

        public bool TryGetBugId(ulong targetId, out int bugId) {
            bugId = 0;
            if ((targetId & TAG_MASK) != BUG_TAG)
                return false;

            bugId = (int)(targetId & ID_MASK);
            return true;
        }
    }
}
