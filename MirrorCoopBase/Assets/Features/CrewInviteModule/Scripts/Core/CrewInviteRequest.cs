namespace Features.CrewInviteModule.Scripts {
    // Everything the invite rule needs: the inviting player's Steam side and the crew as the server reports it.
    public readonly struct CrewInviteRequest {
        public CrewInviteRequest(bool hasSteamLobby, bool isOverlayEnabled, bool isJoinWindowOpen, int playerCount, int playerSlots) {
            HasSteamLobby = hasSteamLobby;
            IsOverlayEnabled = isOverlayEnabled;
            IsJoinWindowOpen = isJoinWindowOpen;
            PlayerCount = playerCount;
            PlayerSlots = playerSlots;
        }

        public bool HasSteamLobby { get; }
        public bool IsOverlayEnabled { get; }
        public bool IsJoinWindowOpen { get; }
        public int PlayerCount { get; }
        public int PlayerSlots { get; }
    }
}
