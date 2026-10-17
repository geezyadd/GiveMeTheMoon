using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The state of one A* search over a slalom grid: the best cost and parent of every point, the points already
    // closed and the open heap ordered by cost plus the octile distance to the goal.
    public sealed class FieldSlalomSearch {
        private const int NO_PARENT = -1;
        private const int STRAIGHT_COST = 10;
        private const int DIAGONAL_COST = 14;

        private readonly bool[] _isClosed;

        public FieldSlalomGrid Grid { get; }
        public Vector2Int Goal { get; }
        public int[] Costs { get; }
        public int[] Parents { get; }
        public FieldSlalomHeap Heap { get; } = new();

        public FieldSlalomSearch(FieldSlalomGrid grid, Vector2Int goal) {
            Grid = grid;
            Goal = goal;
            int count = grid.Size.x * grid.Size.y;
            Costs = new int[count];
            Parents = new int[count];
            _isClosed = new bool[count];
            System.Array.Fill(Costs, int.MaxValue);
            System.Array.Fill(Parents, NO_PARENT);
        }

        public static int StepCost(Vector2Int step) =>
            step.x != 0 && step.y != 0 ? DIAGONAL_COST : STRAIGHT_COST;

        public bool IsClosed(int index) =>
            _isClosed[index];

        public void Close(int index) =>
            _isClosed[index] = true;

        public void Open(int index, int parent, int cost) {
            Costs[index] = cost;
            Parents[index] = parent;
            Heap.Push(cost + Estimate(index), index);
        }

        // Octile distance: never more than the real cost, so the first time the goal closes its way is the shortest.
        private int Estimate(int index) {
            int dx = Mathf.Abs(index % Grid.Size.x - Goal.x);
            int dy = Mathf.Abs(index / Grid.Size.x - Goal.y);
            return STRAIGHT_COST * Mathf.Max(dx, dy) + (DIAGONAL_COST - STRAIGHT_COST) * Mathf.Min(dx, dy);
        }
    }
}
