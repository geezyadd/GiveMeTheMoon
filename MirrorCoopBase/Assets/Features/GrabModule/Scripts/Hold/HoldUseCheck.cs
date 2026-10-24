namespace Features.GrabModule.Scripts.Hold {
    // What the server sees this step for a running hold: the target can still be used, the player is in reach and
    // which item is in the hand now.
    public readonly struct HoldUseCheck {
        public bool IsTargetUsable { get; }
        public bool IsInReach { get; }
        public uint HeldItemNetId { get; }

        public HoldUseCheck(bool isTargetUsable, bool isInReach, uint heldItemNetId) {
            IsTargetUsable = isTargetUsable;
            IsInReach = isInReach;
            HeldItemNetId = heldItemNetId;
        }
    }
}
