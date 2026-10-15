namespace Features.CrewInviteModule.Scripts.Core {
    public readonly struct CrewInviteRequest {
        public bool HasSteamLobby { get; }
        public bool IsOverlayEnabled { get; }
        public bool IsJoinWindowOpen { get; }
        public int PlayerCount { get; }
        public int PlayerSlots { get; }

        public CrewInviteRequest(bool hasSteamLobby, bool isOverlayEnabled, bool isJoinWindowOpen, int playerCount, int playerSlots) {
            HasSteamLobby = hasSteamLobby;
            IsOverlayEnabled = isOverlayEnabled;
            IsJoinWindowOpen = isJoinWindowOpen;
            PlayerCount = playerCount;
            PlayerSlots = playerSlots;
        }
    }
}
