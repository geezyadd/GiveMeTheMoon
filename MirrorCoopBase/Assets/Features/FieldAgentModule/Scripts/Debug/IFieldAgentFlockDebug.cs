#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Spawn;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: the "Spawn test agents (N)" flock. Public for the loop tests.
    public interface IFieldAgentFlockDebug {
        public IReadOnlyList<int> FlockIds { get; }
        public int OrbitingCount { get; }
        public FieldAgentSpawnCover LastSpawnCover { get; }
        public int DebugSpawnTestFlock(int count);
        public void DebugRemoveAll();
    }
}
#endif
