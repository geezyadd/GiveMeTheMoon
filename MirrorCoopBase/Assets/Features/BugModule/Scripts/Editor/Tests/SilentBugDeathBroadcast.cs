using System.Collections.Generic;
using Features.BugModule.Scripts.Network;

namespace Features.BugModule.Scripts.Editor.Tests {
    // Records the deaths the server would send to every peer.
    public sealed class SilentBugDeathBroadcast : IBugDeathBroadcast {
        public List<int> Deaths { get; } = new();

        public void ServerBroadcastDeath(int bugId) =>
            Deaths.Add(bugId);
    }
}
