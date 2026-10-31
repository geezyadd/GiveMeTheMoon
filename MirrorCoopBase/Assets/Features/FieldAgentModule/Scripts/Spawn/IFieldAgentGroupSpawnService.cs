using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Server: a group of agents of one kind appearing together, hidden from every player or at a given point.
    public interface IFieldAgentGroupSpawnService {
        public FieldAgentSpawnCover SpawnHiddenGroup(FieldAgentProfile profile, int count, Vector3 center, int seed, Func<IFieldAgentBehaviour> createBehaviour, List<int> spawnedIds);
        // A group scattered round a given point, seen or not (a nest).
        public void SpawnGroupAt(FieldAgentProfile profile, int count, Vector3 point, int seed, Func<IFieldAgentBehaviour> createBehaviour, List<int> spawnedIds);
    }
}
