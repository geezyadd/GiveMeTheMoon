using Features.ConstellationModule.Scripts.Configurations;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    // How a shown figure gives way to the next one: it fades out as a whole while the new reveal starts a little later
    // (a cross-fade); picking the shown figure again waits for the whole fade, so the replay starts from a dark sky.
    public sealed class ConstellationFadeRule : IConstellationFadeRule {
        private readonly ConstellationHighlightConfiguration _constellationHighlightConfiguration;

        public ConstellationFadeRule(ConstellationHighlightConfiguration constellationHighlightConfiguration) =>
            _constellationHighlightConfiguration = constellationHighlightConfiguration;

        public float GetRevealStartDelaySeconds(bool hasShown, bool isReplay) {
            if (hasShown == false)
                return 0f;
            return isReplay ? _constellationHighlightConfiguration.FadeOutSeconds : _constellationHighlightConfiguration.SwitchRevealDelaySeconds;
        }

        public float GetFadeOutAlpha(float fadingSeconds) =>
            1f - Mathf.SmoothStep(0f, 1f, fadingSeconds / _constellationHighlightConfiguration.FadeOutSeconds);

        public bool IsFadeOutOver(float fadingSeconds) =>
            fadingSeconds >= _constellationHighlightConfiguration.FadeOutSeconds;
    }
}
