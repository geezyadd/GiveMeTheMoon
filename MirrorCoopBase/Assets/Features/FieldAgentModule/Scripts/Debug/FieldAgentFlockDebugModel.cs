#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Spawn;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: the agents of the debug flock, the ones still seeking the ship, and how the last group
    // was hidden. Written by FieldAgentFlockDebugSystem, read by the tests.
    public sealed class FieldAgentFlockDebugModel {
        private readonly List<int> _flockIds = new();
        private readonly List<int> _seekingIds = new();

        public IReadOnlyList<int> FlockIds => _flockIds;
        public int OrbitingCount => _flockIds.Count - _seekingIds.Count;
        public FieldAgentSpawnCover LastSpawnCover { get; private set; }
        public int SpawnSeed { get; private set; }

        public bool IsSeeking(int id) =>
            _seekingIds.Contains(id);

        public int TakeSpawnSeed() =>
            ++SpawnSeed;

        public void AddSeekingGroup(IReadOnlyList<int> ids, FieldAgentSpawnCover cover) {
            foreach (int id in ids) {
                _flockIds.Add(id);
                _seekingIds.Add(id);
            }

            LastSpawnCover = cover;
        }

        public void MarkOrbiting(int id) =>
            _seekingIds.Remove(id);

        public void Forget(int id) {
            _flockIds.Remove(id);
            _seekingIds.Remove(id);
        }

        public void Clear() {
            _flockIds.Clear();
            _seekingIds.Clear();
        }
    }
}
#endif
