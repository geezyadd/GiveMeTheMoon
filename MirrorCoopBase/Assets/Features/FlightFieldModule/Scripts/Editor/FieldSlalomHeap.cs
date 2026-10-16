using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Editor {
    // A binary min-heap of grid point indices by an integer cost, for the slalom search. A point may sit in it more
    // than once with older, higher costs; the search skips those.
    public sealed class FieldSlalomHeap {
        private readonly List<(int cost, int index)> _items = new();

        public int Count => _items.Count;

        public void Push(int cost, int index) {
            _items.Add((cost, index));
            int child = _items.Count - 1;
            while (child > 0) {
                int parent = (child - 1) / 2;
                if (_items[parent].cost <= _items[child].cost)
                    return;

                Swap(parent, child);
                child = parent;
            }
        }

        public (int cost, int index) Pop() {
            (int cost, int index) top = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);
            SiftDown();
            return top;
        }

        private void SiftDown() {
            int parent = 0;
            while (true) {
                int smallest = Smallest(parent, parent * 2 + 1);
                smallest = Smallest(smallest, parent * 2 + 2);
                if (smallest == parent)
                    return;

                Swap(parent, smallest);
                parent = smallest;
            }
        }

        private int Smallest(int current, int candidate) =>
            candidate < _items.Count && _items[candidate].cost < _items[current].cost ? candidate : current;

        private void Swap(int first, int second) =>
            (_items[first], _items[second]) = (_items[second], _items[first]);
    }
}
