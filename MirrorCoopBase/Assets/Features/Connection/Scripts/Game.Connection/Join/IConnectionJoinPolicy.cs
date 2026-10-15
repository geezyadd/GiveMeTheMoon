using System;

namespace Game.Connection
{
    // Server: the join rule applied to the session as it is right now.
    public interface IConnectionJoinPolicy
    {
        // Raised whenever an input of the rule may have changed: run window, map change, players.
        public event Action OnJoinStateChanged;

        public bool IsOpenForNewPlayers { get; }

        public bool IsJoinWindowOpen { get; }

        public int PlayerCount { get; }

        public int PlayerSlots { get; }

        public ConnectionRejectReason EvaluateJoin(string version, ulong steamId);
    }
}
