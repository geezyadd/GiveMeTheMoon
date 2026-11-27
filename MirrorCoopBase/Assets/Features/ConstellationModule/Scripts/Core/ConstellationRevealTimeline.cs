using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    // When each star of a figure lights up and each line is drawn, counted in seconds from the reveal's start; made by
    // ConstellationRevealTimelineFactory from the order of the constellation's lines.
    public sealed class ConstellationRevealTimeline {
        private readonly float[] _starStartSeconds;
        private readonly float[] _lineStartSeconds;
        private readonly float[] _lineDrawSeconds;
        private readonly float _starFadeInSeconds;
        private readonly float _starFlashOvershoot;

        public int StarCount => _starStartSeconds.Length;
        public int LineCount => _lineStartSeconds.Length;

        public ConstellationRevealTimeline(float[] starStartSeconds, float[] lineStartSeconds, float[] lineDrawSeconds, float starFadeInSeconds, float starFlashOvershoot) {
            _starStartSeconds = starStartSeconds;
            _lineStartSeconds = lineStartSeconds;
            _lineDrawSeconds = lineDrawSeconds;
            _starFadeInSeconds = starFadeInSeconds;
            _starFlashOvershoot = starFlashOvershoot;
        }

        // 0 before the star lights up, 1 once it has; on the way it eases in and flashes past 1 halfway (the sine is
        // zero at both ends, so a lit star is exactly at its full look).
        public float GetStarLight(int star, float revealSeconds) {
            float progress = GetProgress(revealSeconds - _starStartSeconds[star], _starFadeInSeconds);
            if (progress >= 1f)
                return 1f;

            float eased = progress * progress * (3f - 2f * progress);
            return eased + _starFlashOvershoot * Mathf.Sin(Mathf.PI * progress);
        }

        // Share of the line drawn from its first star: 0 before it starts, 1 once it has reached its second star.
        public float GetLineProgress(int line, float revealSeconds) =>
            GetProgress(revealSeconds - _lineStartSeconds[line], _lineDrawSeconds[line]);

        private static float GetProgress(float elapsedSeconds, float durationSeconds) {
            if (durationSeconds <= 0f)
                return elapsedSeconds >= 0f ? 1f : 0f;
            return Mathf.Clamp01(elapsedSeconds / durationSeconds);
        }
    }
}
