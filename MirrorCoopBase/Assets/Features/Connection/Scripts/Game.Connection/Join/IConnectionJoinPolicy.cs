namespace Game.Connection
{
    // Server: the join rule applied to the session as it is right now.
    public interface IConnectionJoinPolicy
    {
        public bool IsOpenForNewPlayers { get; }

        public bool IsJoinWindowOpen { get; }

        public int PlayerCount { get; }

        public int PlayerSlots { get; }

        public ConnectionRejectReason EvaluateJoin(string version, ulong steamId);
    }
}
