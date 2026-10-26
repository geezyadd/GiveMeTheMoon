namespace Features.DebugModule.Contracts {
    // The common reasons a debug action is unavailable, shown as the disabled button's hint.
    public static class DebugActionReasons {
        public const string HOST_ONLY = "Host only: the server does this.";
        public const string NO_SHIP = "No ship in this session yet.";
        public const string NOT_FLYING = "Only while the ship flies.";
        public const string NO_LOCAL_PLAYER = "No local player spawned.";
    }
}
