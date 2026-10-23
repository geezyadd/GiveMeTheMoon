using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Finds a way for a ship that needs the given clearance on every side: an A* search (octile steps) over the shut flags
    // of a fine grid of the field plane, then the grid steps are pulled straight wherever a straight line keeps the
    // clearance (measured exactly), so the length is close to the one a pilot flies. Two rocks exactly the ship gap
    // apart leave a passage of no width that no grid point hits, so the grid asks for the clearance less half a grid
    // diagonal: the way may pass that much closer to an obstacle than asked.
    public sealed class FieldSlalomPathFinder {
        private const float GRID_STEP_METERS = 2.5f;
        private const float HALF_DIAGONAL = 0.7072f;
        private const float LINE_SAMPLE_METERS = 2.5f;
        // How many grid points ahead a straight shortcut is first tried (a power of two, halved on every miss).
        private const int MAX_SHORTCUT_POINTS = 64;

        private static readonly Vector2Int[] _steps = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        public static float ToleranceMeters => GRID_STEP_METERS * HALF_DIAGONAL;

        public bool TryFind(FieldSlalomObstacles obstacles, Vector2 start, Vector2 goal, float shipClearance, List<Vector2> path) {
            path.Clear();
            float clearance = shipClearance - ToleranceMeters;
            FieldSlalomGrid grid = new(obstacles, GRID_STEP_METERS, clearance);
            FieldSlalomSearch search = new(grid, grid.ToPoint(goal));
            int startIndex = grid.Index(grid.ToPoint(start));
            int goalIndex = grid.Index(search.Goal);
            search.Open(startIndex, startIndex, 0);
            while (search.Heap.Count > 0 && !search.IsClosed(goalIndex))
                Expand(search);

            if (!search.IsClosed(goalIndex))
                return false;


            List<Vector2> steps = TraceBack(grid, search.Parents, goalIndex);
            steps[0] = start;
            steps[^1] = goal;
            PullStraight(obstacles, steps, clearance, path);
            return true;
        }

        // Smallest distance to an obstacle surface along the path, sampled at the line sample step.
        public float MinClearance(FieldSlalomObstacles obstacles, IReadOnlyList<Vector2> path, float limit) {
            float min = limit;
            for (int i = 1; i < path.Count; i++)
                min = Mathf.Min(min, MinClearanceOnLine(obstacles, path[i - 1], path[i], limit));

            return min;
        }

        private static void Expand(FieldSlalomSearch search) {
            (long _, int index) = search.Heap.Pop();
            if (search.IsClosed(index))
                return;

            search.Close(index);
            FieldSlalomGrid grid = search.Grid;
            Vector2Int point = new(index % grid.Size.x, index / grid.Size.x);
            foreach (Vector2Int step in _steps) {
                Vector2Int next = point + step;
                if (!grid.IsInside(next) || grid.IsShut(next))
                    continue;

                int nextIndex = grid.Index(next);
                int cost = search.Costs[index] + FieldSlalomSearch.StepCost(step);
                if (!search.IsClosed(nextIndex) && cost < search.Costs[nextIndex])
                    search.Open(nextIndex, index, cost);
            }
        }

        private static List<Vector2> TraceBack(FieldSlalomGrid grid, int[] parents, int goalIndex) {
            List<Vector2> steps = new();
            int index = goalIndex;
            while (parents[index] != index) {
                steps.Add(grid.ToPlane(new Vector2Int(index % grid.Size.x, index / grid.Size.x)));
                index = parents[index];
            }

            steps.Add(grid.ToPlane(new Vector2Int(index % grid.Size.x, index / grid.Size.x)));
            steps.Reverse();
            return steps;
        }

        // From each kept point, a straight line to the point the shortcut reach ahead, else half as far, and so on down
        // to the next point: a few exact lines per kept point, whatever the way looks like.
        private void PullStraight(FieldSlalomObstacles obstacles, List<Vector2> steps, float clearance, List<Vector2> path) {
            int anchor = 0;
            path.Add(steps[0]);
            while (anchor < steps.Count - 1) {
                int ahead = MAX_SHORTCUT_POINTS;
                while (ahead > 1 && !IsClearLine(obstacles, steps, anchor, ahead, clearance))
                    ahead /= 2;

                anchor = Mathf.Min(steps.Count - 1, anchor + ahead);
                path.Add(steps[anchor]);
            }
        }

        private static bool IsClearLine(FieldSlalomObstacles obstacles, List<Vector2> steps, int anchor, int ahead, float clearance) {
            int target = Mathf.Min(steps.Count - 1, anchor + ahead);
            return MinClearanceOnLine(obstacles, steps[anchor], steps[target], clearance) >= clearance;
        }

        private static float MinClearanceOnLine(FieldSlalomObstacles obstacles, Vector2 from, Vector2 to, float limit) {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / LINE_SAMPLE_METERS));
            float min = limit;
            for (int i = 0; i <= samples; i++)
                min = Mathf.Min(min, obstacles.Clearance(Vector2.Lerp(from, to, i / (float)samples), limit));

            return min;
        }
    }
}
