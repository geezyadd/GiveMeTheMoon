using Features.GameCoreModule.Contracts;

namespace Game.Connection
{
    // Everything the join rule needs to know about one player knocking and the session they knock on.
    public readonly struct JoinRequest
    {
        public JoinRequest(
            bool versionMatches,
            bool hasValidSteamId,
            bool isInLobby,
            bool isChangingMap,
            RunJoinWindow runWindow,
            int playerCount,
            int playerSlots)
        {
            VersionMatches = versionMatches;
            HasValidSteamId = hasValidSteamId;
            IsInLobby = isInLobby;
            IsChangingMap = isChangingMap;
            RunWindow = runWindow;
            PlayerCount = playerCount;
            PlayerSlots = playerSlots;
        }

        public bool VersionMatches { get; }
        public bool HasValidSteamId { get; }
        public bool IsInLobby { get; }
        public bool IsChangingMap { get; }
        public RunJoinWindow RunWindow { get; }
        public int PlayerCount { get; }
        public int PlayerSlots { get; }
    }
}
