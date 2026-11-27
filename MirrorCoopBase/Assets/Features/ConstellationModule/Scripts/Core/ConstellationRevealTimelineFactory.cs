using System.Collections.Generic;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    // Lays a figure's reveal out step after step in the order of its lines: a line's first star lights up if it is not
    // lit yet, then the line is drawn, then its second star lights up if it is not lit yet. A pause follows every lit
    // star, and a line ending on a lit star (a closed loop) is followed by the pause alone. Stars on no line light up
    // after the last line.
    public sealed class ConstellationRevealTimelineFactory : IConstellationRevealTimelineFactory {
        private readonly ConstellationHighlightConfiguration _constellationHighlightConfiguration;

        public ConstellationRevealTimelineFactory(ConstellationHighlightConfiguration constellationHighlightConfiguration) =>
            _constellationHighlightConfiguration = constellationHighlightConfiguration;

        public ConstellationRevealTimeline Create(ConstellationDefinition constellation) {
            IReadOnlyList<ConstellationStar> stars = constellation.Stars;
            IReadOnlyList<ConstellationLine> lines = constellation.Lines;
            float[] starStartSeconds = new float[stars.Count];
            bool[] isStarScheduled = new bool[stars.Count];
            float[] lineStartSeconds = new float[lines.Count];
            float[] lineDrawSeconds = new float[lines.Count];
            float seconds = 0f;
            for (int i = 0; i < lines.Count; i++) {
                ConstellationLine line = lines[i];
                if (isStarScheduled[line.FromStar] == false)
                    seconds = ScheduleStar(starStartSeconds, isStarScheduled, line.FromStar, seconds);
                lineStartSeconds[i] = seconds;
                lineDrawSeconds[i] = GetLineDrawSeconds(stars[line.FromStar], stars[line.ToStar]);
                seconds += lineDrawSeconds[i];
                seconds = isStarScheduled[line.ToStar]
                    ? seconds + _constellationHighlightConfiguration.PauseBetweenStepsSeconds
                    : ScheduleStar(starStartSeconds, isStarScheduled, line.ToStar, seconds);
            }
            for (int i = 0; i < stars.Count; i++) {
                if (isStarScheduled[i] == false)
                    seconds = ScheduleStar(starStartSeconds, isStarScheduled, i, seconds);
            }
            return new ConstellationRevealTimeline(starStartSeconds, lineStartSeconds, lineDrawSeconds, _constellationHighlightConfiguration.StarFadeInSeconds, _constellationHighlightConfiguration.StarFlashOvershoot);
        }

        private float ScheduleStar(float[] starStartSeconds, bool[] isStarScheduled, int star, float seconds) {
            starStartSeconds[star] = seconds;
            isStarScheduled[star] = true;
            return seconds + _constellationHighlightConfiguration.StarFadeInSeconds + _constellationHighlightConfiguration.PauseBetweenStepsSeconds;
        }

        private float GetLineDrawSeconds(ConstellationStar from, ConstellationStar to) =>
            Vector3.Angle(from.CelestialDirection, to.CelestialDirection) / _constellationHighlightConfiguration.LineDrawDegreesPerSecond;
    }
}
