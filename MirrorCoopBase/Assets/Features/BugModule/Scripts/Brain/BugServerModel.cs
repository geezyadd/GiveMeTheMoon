using System.Collections.Generic;

namespace Features.BugModule.Scripts.Brain {
    // Server: every live bug by its field agent id, in the order they came, and the seed of the next spawned group.
    // Written by the bug services and systems, read by the tests. Bound in DataInstaller.
    public sealed class BugServerModel {
        private readonly Dictionary<int, BugAgent> _bugs = new();
        private readonly List<int> _ids = new();

        public IReadOnlyList<int> Ids => _ids;
        public int Count => _ids.Count;
        public int SpawnSeed { get; private set; }

        public bool TryGet(int id, out BugAgent bug) =>
            _bugs.TryGetValue(id, out bug);

        public void Add(BugAgent bug) {
            _bugs.Add(bug.Id, bug);
            _ids.Add(bug.Id);
        }

        public void Remove(int id) {
            if (_bugs.Remove(id))
                _ids.Remove(id);
        }

        public void Clear() {
            _bugs.Clear();
            _ids.Clear();
        }

        public int TakeSpawnSeed() =>
            ++SpawnSeed;
    }
}
