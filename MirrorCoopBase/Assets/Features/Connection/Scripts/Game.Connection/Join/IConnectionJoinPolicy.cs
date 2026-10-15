namespace Game.Connection
{
    // Server: the join rule applied to the session as it is right now.
    public interface IConnectionJoinPolicy
    {
        public bool IsOpenForNewPlayers { get; }

        public ConnectionRejectReason EvaluateJoin(string version, ulong steamId);
    }
}
