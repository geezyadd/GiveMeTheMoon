using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Spawn;
using UnityEngine;

namespace Features.BugModule.Scripts.Spawn {
    public interface IBugSpawnService {
        // Server: a group of bugs appears at a point no player sees and flies at the ship; how it was hidden, None
        // when no hidden point was found (and no bug spawned).
        public FieldAgentSpawnCover ServerSpawnGroup(int count, List<int> spawnedIds);
        // Server: a group of bugs appears scattered round a given point (a nest) and flies at the ship like any group.
        public void ServerSpawnGroupAt(int count, Vector3 point, List<int> spawnedIds);
    }
}
