namespace Features.ConstellationModule.Scripts.Core {
    public interface IConstellationFadeRule {
        public float GetRevealStartDelaySeconds(bool hasShown, bool isReplay);
        public float GetFadeOutAlpha(float fadingSeconds);
        public bool IsFadeOutOver(float fadingSeconds);
    }
}
