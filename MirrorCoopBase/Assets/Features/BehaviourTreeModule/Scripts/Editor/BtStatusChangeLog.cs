using System.Collections.Generic;

namespace Features.BehaviourTreeModule.Editor {
    // The last status changes of one tree's nodes, newest first, stamped with the tree clock.
    public sealed class BtStatusChangeLog {
        private readonly int _capacity;
        private readonly BtStatusChange[] _changes;
        private readonly List<BtNode> _watchedNodes = new();

        private BtTree _tree;
        private int _newestIndex = -1;

        public BtStatusChangeLog(int capacity) {
            _capacity = capacity;
            _changes = new BtStatusChange[capacity];
        }

        public int Count { get; private set; }

        public void Watch(BtTree tree) {
            Unwatch();
            _tree = tree;
            WatchBranch(tree.Root);
        }

        public void Unwatch() {
            for (int i = 0; i < _watchedNodes.Count; i++)
                _watchedNodes[i].OnStatusChanged -= HandleStatusChanged;

            _watchedNodes.Clear();
            _tree = null;
            _newestIndex = -1;
            Count = 0;
        }

        // 0 is the newest change.
        public BtStatusChange GetChange(int age) =>
            _changes[(_newestIndex - age + _capacity) % _capacity];

        private void WatchBranch(BtNode node) {
            node.OnStatusChanged += HandleStatusChanged;
            _watchedNodes.Add(node);
            for (int i = 0; i < node.Children.Count; i++)
                WatchBranch(node.Children[i]);
        }

        private void HandleStatusChanged(BtNode node) {
            _newestIndex = (_newestIndex + 1) % _capacity;
            _changes[_newestIndex] = new BtStatusChange(_tree.Clock.ElapsedSeconds, node.Name, node.Status);
            if (Count < _capacity)
                Count++;
        }
    }
}
