using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Branch centerlines off a main centerline. An alternative follows the main centerline between its two ends and
    // is pushed sideways by a half sine, so it leaves and rejoins it. A dead end leaves at an angle, forward and to
    // the side, with a slight bend, and simply stops.
    public sealed class FieldBranchPlanner : IFieldBranchPlanner {
        private const float SAMPLE_STEP_CELLS = 0.5f;
        private const float DEAD_END_FORWARD_SHARE = 0.6f;
        private const float DEAD_END_SIDE_SHARE = 0.8f;
        private const float DEAD_END_BEND_SHARE = 0.15f;
        private const int FORWARD_REACH_CELLS = 4;

        private readonly IFieldPathRasterizer _fieldPathRasterizer;

        public FieldBranchPlanner(IFieldPathRasterizer fieldPathRasterizer) =>
            _fieldPathRasterizer = fieldPathRasterizer;

        // Span: x = the index it leaves the main centerline at, y = the index it comes back at.
        public List<Vector2Int> PlanAlternative(FieldGenerationContext context, IReadOnlyList<Vector2Int> main, Vector2Int span, int side, float offsetCells, int inset) {
            Vector2 normal = Normal(main[span.y] - main[span.x]) * side;
            int count = span.y - span.x;
            List<Vector2Int> path = new() { main[span.x] };
            int samples = Mathf.CeilToInt(count / SAMPLE_STEP_CELLS);
            for (int i = 1; i <= samples; i++) {
                float t = i / (float)samples;
                Vector2 onMain = main[span.x + Mathf.RoundToInt(t * count)];
                _fieldPathRasterizer.Append(path, _fieldPathRasterizer.ClampInside(onMain + normal * (offsetCells * Mathf.Sin(Mathf.PI * t)), inset, context.Frame.Size));
            }

            _fieldPathRasterizer.Append(path, main[span.y]);
            return path;
        }

        public List<Vector2Int> PlanDeadEnd(FieldGenerationContext context, IReadOnlyList<Vector2Int> main, int fromIndex, int side, float lengthCells, int inset) {
            Vector2 forward = Forward(main, fromIndex);
            Vector2 normal = new Vector2(-forward.y, forward.x) * side;
            Vector2 direction = (forward * DEAD_END_FORWARD_SHARE + normal * DEAD_END_SIDE_SHARE).normalized;
            Vector2 bend = new Vector2(-direction.y, direction.x) * (lengthCells * DEAD_END_BEND_SHARE);
            Vector2 start = main[fromIndex];
            List<Vector2Int> path = new() { main[fromIndex] };
            int samples = Mathf.CeilToInt(lengthCells / SAMPLE_STEP_CELLS);
            for (int i = 1; i <= samples; i++) {
                float t = i / (float)samples;
                Vector2 point = start + direction * (lengthCells * t) + bend * Mathf.Sin(Mathf.PI * t);
                _fieldPathRasterizer.Append(path, _fieldPathRasterizer.ClampInside(point, inset, context.Frame.Size));
            }

            return path;
        }

        // The main centerline's direction around the index, from a few cells back to a few cells ahead.
        private static Vector2 Forward(IReadOnlyList<Vector2Int> main, int index) {
            Vector2 delta = main[Mathf.Min(main.Count - 1, index + FORWARD_REACH_CELLS)] - main[Mathf.Max(0, index - FORWARD_REACH_CELLS)];
            return delta.sqrMagnitude > 0f ? delta.normalized : Vector2.right;
        }

        private static Vector2 Normal(Vector2 axis) {
            Vector2 direction = axis.sqrMagnitude > 0f ? axis.normalized : Vector2.right;
            return new Vector2(-direction.y, direction.x);
        }
    }
}
