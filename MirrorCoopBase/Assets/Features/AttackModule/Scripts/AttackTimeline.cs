using UnityEngine;

namespace Features.AttackModule {
    // An attack over time, pure: how strongly its limb reaches for the target, whether it may hit, whether it is over.
    public static class AttackTimeline {
        // 0 until the reach starts (the plain animation, its wind-up), eases in to 1 by the full reach, holds 1 until
        // the release, eases out to 0 by the reach's end.
        public static float ReachWeight(in AttackTiming timing, float secondsSinceStart) {
            float t = secondsSinceStart;
            if (t <= timing.ReachStartSeconds || t >= timing.ReachEndSeconds)
                return 0f;
            if (t < timing.ReachFullSeconds)
                return Mathf.SmoothStep(0f, 1f,
                    (t - timing.ReachStartSeconds) / (timing.ReachFullSeconds - timing.ReachStartSeconds));
            if (t <= timing.ReachReleaseSeconds)
                return 1f;

            float fadeSeconds = timing.ReachEndSeconds - timing.ReachReleaseSeconds;
            return fadeSeconds > 0f ? Mathf.SmoothStep(1f, 0f, (t - timing.ReachReleaseSeconds) / fadeSeconds) : 0f;
        }

        public static bool IsInHitWindow(in AttackTiming timing, float secondsSinceStart) =>
            secondsSinceStart >= timing.HitStartSeconds && secondsSinceStart <= timing.HitEndSeconds;

        public static bool IsPastHitWindow(in AttackTiming timing, float secondsSinceStart) =>
            secondsSinceStart > timing.HitEndSeconds;

        public static bool IsOver(in AttackTiming timing, float secondsSinceStart) =>
            secondsSinceStart >= timing.DurationSeconds;
    }
}
