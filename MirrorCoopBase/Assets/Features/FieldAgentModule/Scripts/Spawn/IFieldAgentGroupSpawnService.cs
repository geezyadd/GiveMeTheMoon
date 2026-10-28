using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Server: a group of agents of one kind appearing together at a point no player sees.
    public interface IFieldAgentGroupSpawnService {
        public FieldAgentSpawnCover SpawnHiddenGroup(FieldAgentProfile profile, int count, Vector3 center, int seed, Func<IFieldAgentBehaviour> createBehaviour, List<int> spawnedIds);
    }
}
