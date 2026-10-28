namespace Features.ShipModule.Scripts.Overboard {
    // Packs a drifting player's network id into a weapon target id: the top byte marks a player overboard (field
    // rocks keep 0xF1, ore 0xF3, the debug dummy 0xDE), the low 32 bits hold the network id, the same on every peer.
    public sealed class ShipOverboardWeaponIds : IShipOverboardWeaponIds {
        private const int TAG_SHIFT = 56;
        private const ulong PLAYER_TAG = 0xF4UL << TAG_SHIFT;
        private const ulong TAG_MASK = 0xFFUL << TAG_SHIFT;
        private const ulong ID_MASK = 0xFFFFFFFFUL;

        public ulong GetTargetId(uint playerNetId) =>
            PLAYER_TAG | playerNetId;

        public bool TryGetPlayerNetId(ulong targetId, out uint playerNetId) {
            playerNetId = 0;
            if ((targetId & TAG_MASK) != PLAYER_TAG)
                return false;

            playerNetId = (uint)(targetId & ID_MASK);
            return true;
        }
    }
}
