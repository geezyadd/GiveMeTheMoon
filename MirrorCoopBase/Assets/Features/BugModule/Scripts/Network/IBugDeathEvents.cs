using System;

namespace Features.BugModule.Scripts.Network {
    // Every peer (the host too): fires once per bug the server killed, with its field agent id.
    public interface IBugDeathEvents {
        public event Action<int> OnBugDied;
    }
}
