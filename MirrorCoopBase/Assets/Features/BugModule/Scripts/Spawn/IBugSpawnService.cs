using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Spawn;

namespace Features.BugModule.Scripts.Spawn {
    public interface IBugSpawnService {
        // Server: a group of bugs appears at a point no player sees and flies at the ship; how it was hidden, None
        // when no hidden point was found (and no bug spawned).
        public FieldAgentSpawnCover ServerSpawnGroup(int count, List<int> spawnedIds);
    }
}
