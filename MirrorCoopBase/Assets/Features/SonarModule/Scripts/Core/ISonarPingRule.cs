namespace Features.SonarModule.Scripts.Core {
    public interface ISonarPingRule {
        public bool CanPing(float now, float lastPingAt);
        public float GetCooldownShare(float now, float lastPingAt);
        public float GetWaveShare(float now, float pingStartedAt);
        public float GetEchoAlpha(float now, float litAt);
    }
}
