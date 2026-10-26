using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // The ping game's timing: the cooldown between pings, the wave running from the ship to the scope's edge and an
    // echo fading after the wave lit it.
    public sealed class SonarPingRule : ISonarPingRule {
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarPingRule(SonarConfiguration sonarConfiguration) =>
            _sonarConfiguration = sonarConfiguration;

        public bool CanPing(float now, float lastPingAt) =>
            now - lastPingAt >= _sonarConfiguration.PingCooldownSeconds;

        // 1 = ready again.
        public float GetCooldownShare(float now, float lastPingAt) =>
            Mathf.Clamp01((now - lastPingAt) / _sonarConfiguration.PingCooldownSeconds);

        // 0 at the ship, 1 at the scope's edge; above 1 the wave is gone.
        public float GetWaveShare(float now, float pingStartedAt) =>
            Mathf.Max(0f, (now - pingStartedAt) / _sonarConfiguration.PingWaveSeconds);

        public float GetEchoAlpha(float now, float litAt) =>
            Mathf.Clamp01(1f - (now - litAt) / _sonarConfiguration.EchoFadeSeconds);
    }
}
