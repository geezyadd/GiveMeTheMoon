namespace Features.ParcelModule.Scripts.Services {
    // Packs a parcel crate's net id into a weapon target id: the top byte marks a parcel (field rocks keep 0xF1, the
    // debug dummy 0xDE, bugs 0xB0), the low 32 bits hold the net id.
    public sealed class ParcelWeaponIds : IParcelWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong PARCEL_TAG = 0xC0UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong ID_MASK = 0xFFFFFFFFUL;

        public ulong GetTargetId(uint crateNetId) =>
            PARCEL_TAG | crateNetId;

        public bool TryGetCrateNetId(ulong targetId, out uint crateNetId) {
            crateNetId = 0;
            if ((targetId & TAG_MASK) != PARCEL_TAG)
                return false;

            crateNetId = (uint)(targetId & ID_MASK);
            return true;
        }
    }
}
