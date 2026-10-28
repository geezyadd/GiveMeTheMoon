using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Smooth Perlin wander per axis, -1..1, scaled to the profile's share of the top speed; each agent reads its own
    // stretch of the noise through its phase.
    public sealed class FieldAgentWander : IFieldAgentWander {
        private const float AXIS_OFFSET_Y = 31.7f;
        private const float AXIS_OFFSET_Z = 67.3f;

        public Vector3 CalculateWander(float noisePhase, float time, FieldAgentProfile profile) {
            float noiseTime = time * profile.NoiseFrequency;
            Vector3 noise = new(
                Mathf.PerlinNoise(noiseTime, noisePhase) * 2f - 1f,
                Mathf.PerlinNoise(noiseTime + AXIS_OFFSET_Y, noisePhase) * 2f - 1f,
                Mathf.PerlinNoise(noiseTime + AXIS_OFFSET_Z, noisePhase) * 2f - 1f);
            return noise * (profile.NoiseStrength * profile.MaxSpeed);
        }
    }
}
