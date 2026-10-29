using System.Collections.Generic;
using Features.BugModule.Scripts.Spawn;
using Features.FieldAgentModule.Scripts.Spawn;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    public sealed class RecordingBugSpawnService : IBugSpawnService {
        public int Groups { get; private set; }
        public int LastCount { get; private set; }
        public Vector3 LastPoint { get; private set; }

        public FieldAgentSpawnCover ServerSpawnGroup(int count, List<int> spawnedIds) =>
            throw new System.NotSupportedException();

        public void ServerSpawnGroupAt(int count, Vector3 point, List<int> spawnedIds) {
            spawnedIds.Clear();
            Groups++;
            LastCount = count;
            LastPoint = point;
        }
    }
}
