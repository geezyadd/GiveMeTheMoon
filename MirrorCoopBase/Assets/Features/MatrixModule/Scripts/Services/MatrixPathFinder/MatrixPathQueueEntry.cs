using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixPathFinder {
    internal readonly struct MatrixPathQueueEntry {
        public Vector2Int Position { get; }
        public float Priority { get; }
        public long Order { get; }

        public MatrixPathQueueEntry(Vector2Int position, float priority, long order) {
            Position = position;
            Priority = priority;
            Order = order;
        }

        // Equal priorities fall back to the enqueue order, so the same matrix always gives the same path.
        public bool IsBefore(MatrixPathQueueEntry other) {
            int priorityComparison = Priority.CompareTo(other.Priority);
            return priorityComparison != 0
                ? priorityComparison < 0
                : Order < other.Order;
        }
    }
}
