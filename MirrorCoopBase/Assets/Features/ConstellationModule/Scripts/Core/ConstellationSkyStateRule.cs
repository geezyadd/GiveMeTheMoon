using Features.ConstellationModule.Data.Generated;

namespace Features.ConstellationModule.Scripts.Core {
    // What the lobby sky's synced state becomes on a pick or a hide at a network time: the change time is when the
    // figure shown before starts to fade out on every peer, the reveal start is when the new figure's reveal begins
    // (later than the change while it waits for the fade, as the fade rule says). No id means nothing is shown.
    public sealed class ConstellationSkyStateRule : IConstellationSkyStateRule {
        private readonly IConstellationFadeRule _constellationFadeRule;

        public ConstellationSkyStateRule(IConstellationFadeRule constellationFadeRule) =>
            _constellationFadeRule = constellationFadeRule;

        public bool IsShown(ConstellationSkyState state) =>
            string.IsNullOrEmpty(state.ConstellationId) == false;

        public ConstellationSkyState CreateShown(ConstellationSkyState current, string constellationId, double time) {
            bool isReplay = current.ConstellationId == constellationId;
            float revealDelaySeconds = _constellationFadeRule.GetRevealStartDelaySeconds(IsShown(current), isReplay);
            return new ConstellationSkyState {
                ConstellationId = constellationId,
                RevealStartTime = time + revealDelaySeconds,
                ChangeTime = time,
            };
        }

        public ConstellationSkyState CreateHidden(ConstellationSkyState current, double time) =>
            new() {
                ConstellationId = string.Empty,
                RevealStartTime = current.RevealStartTime,
                ChangeTime = time,
            };
    }
}
