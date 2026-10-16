using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // A winding centerline: the straight line from start to end, moved sideways by the channel's offset (which grows
    // in over the profile's split share after the start and fades out over it before the end, so channels part and
    // meet) plus a swing of two sines, the main one and one half as long (75 / 25), with random phases; the swing fades
    // in after the start and out before the end, so the corridor leaves the takeoff and meets station B straight.
    public sealed class FieldMeanderPlanner : IFieldCenterlinePlanner {
        private const float SAMPLE_STEP_CELLS = 0.25f;
        private const float MAIN_SINE_SHARE = 0.75f;
        private const float SECOND_SINE_SHARE = 0.25f;
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;
        private const float MIN_LENGTH = 0.001f;

        private readonly IFieldPathRasterizer _fieldPathRasterizer;

        public FieldMeanderPlanner(IFieldPathRasterizer fieldPathRasterizer) =>
            _fieldPathRasterizer = fieldPathRasterizer;

        public List<Vector2Int> Plan(FieldGenerationContext context, Vector2Int start, Vector2Int end, FieldCorridorShape shape, int inset) {
            List<Vector2Int> path = new() { start };
            Vector2 axis = end - start;
            float length = axis.magnitude;
            float mainPhase = context.Random.Value() * FULL_TURN_RADIANS;
            float secondPhase = context.Random.Value() * FULL_TURN_RADIANS;
            if (length > MIN_LENGTH)
                AppendSwing(context, path, start, axis / length, length, shape, inset, new Vector2(mainPhase, secondPhase));

            _fieldPathRasterizer.Append(path, end);
            return path;
        }

        // Phases: x of the main sine, y of the second one.
        private void AppendSwing(FieldGenerationContext context, List<Vector2Int> path, Vector2 start, Vector2 direction, float length, FieldCorridorShape shape, int inset,
                                 Vector2 phases) {
            Vector2 normal = new(-direction.y, direction.x);
            int samples = Mathf.CeilToInt(length / SAMPLE_STEP_CELLS);
            float fade = context.Profile.MeanderFadeCells;
            float period = shape.MeanderWaves > 0f ? length / shape.MeanderWaves : shape.MeanderPeriodCells;
            float split = context.Profile.ChannelSplitShare * length;
            for (int i = 1; i <= samples; i++) {
                float along = length * i / samples;
                float angle = FULL_TURN_RADIANS * along / period;
                float swing = MAIN_SINE_SHARE * Mathf.Sin(angle + phases.x) + SECOND_SINE_SHARE * Mathf.Sin(2f * angle + phases.y);
                float offset = Fade(along, length, fade) * shape.MeanderAmplitudeCells * swing + Fade(along, length, split) * shape.LateralOffsetCells;
                _fieldPathRasterizer.Append(path, _fieldPathRasterizer.ClampInside(start + direction * along + normal * offset, inset, context.Frame.Size));
            }
        }

        private static float Fade(float along, float length, float fade) {
            if (fade <= 0f)
                return 1f;

            return Mathf.SmoothStep(0f, 1f, along / fade) * Mathf.SmoothStep(0f, 1f, (length - along) / fade);
        }
    }
}
