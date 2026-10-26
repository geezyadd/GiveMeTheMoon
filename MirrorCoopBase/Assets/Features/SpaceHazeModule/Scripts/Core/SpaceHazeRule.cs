using Features.SpaceHazeModule.Data;
using Features.SpaceHazeModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SpaceHazeModule.Scripts.Core {
    // The haze from the configuration and this peer's debug settings, and how much of a thing at a distance it
    // hides. GetHazeShare is the shader's formula (SpaceHaze.shader), so the two must change together.
    public sealed class SpaceHazeRule : ISpaceHazeRule {
        // The shortest fade: HLSL's smoothstep is undefined for a step of zero length.
        private const float MIN_STEP_METERS = 1f;

        private readonly SpaceHazeConfiguration _spaceHazeConfiguration;

        public SpaceHazeRule(SpaceHazeConfiguration spaceHazeConfiguration) =>
            _spaceHazeConfiguration = spaceHazeConfiguration;

        // A debug clear distance moves the whole fade and keeps its configured length.
        public SpaceHazeShape CreateShape(SpaceHazeModel spaceHazeModel) {
            bool isEnabled = _spaceHazeConfiguration.IsEnabled && spaceHazeModel.IsEnabled;
            float fadeMeters = Mathf.Max(_spaceHazeConfiguration.HiddenMeters - _spaceHazeConfiguration.ClearMeters, MIN_STEP_METERS);
            float clearMeters = spaceHazeModel.HasClearMetersOverride ? spaceHazeModel.ClearMetersOverride : _spaceHazeConfiguration.ClearMeters;
            float backdropStartMeters = Mathf.Max(_spaceHazeConfiguration.BackdropStartMeters, clearMeters + fadeMeters);
            float backdropEndMeters = Mathf.Max(_spaceHazeConfiguration.BackdropEndMeters, backdropStartMeters + MIN_STEP_METERS);
            return new SpaceHazeShape(
                clearMeters,
                clearMeters + fadeMeters,
                backdropStartMeters,
                backdropEndMeters,
                isEnabled ? _spaceHazeConfiguration.Strength : 0f);
        }

        public float GetHazeShare(SpaceHazeShape shape, float distanceMeters) {
            float fadeIn = SmoothStep(shape.ClearMeters, shape.HiddenMeters, distanceMeters);
            float fadeOut = SmoothStep(shape.BackdropStartMeters, shape.BackdropEndMeters, distanceMeters);
            return fadeIn * (1f - fadeOut) * shape.Strength;
        }

        // HLSL's smoothstep.
        private static float SmoothStep(float from, float to, float value) {
            float share = Mathf.Clamp01((value - from) / (to - from));
            return share * share * (3f - 2f * share);
        }
    }
}
