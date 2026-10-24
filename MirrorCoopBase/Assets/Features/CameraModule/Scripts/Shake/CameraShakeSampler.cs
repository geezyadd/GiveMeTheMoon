using System.Collections.Generic;
using Features.CameraModule.Contracts;
using UnityEngine;

namespace Features.CameraModule.Scripts.Shake {
    // Moves every shake on by a frame and sums them into one pose: smooth noise per axis, scaled by the profile's
    // degrees and metres and by the shake's current strength or intensity.
    public sealed class CameraShakeSampler : ICameraShakeSampler {
        private const float PITCH_SEED = 11.3f;
        private const float YAW_SEED = 47.9f;
        private const float ROLL_SEED = 83.1f;
        private const float OFFSET_X_SEED = 131.7f;
        private const float OFFSET_Y_SEED = 173.5f;

        public void Advance(CameraShakeModel cameraShakeModel, float deltaTime) {
            IReadOnlyList<CameraShakeOneShot> oneShots = cameraShakeModel.OneShots;
            for (int i = 0; i < oneShots.Count; i++) {
                if (oneShots[i].IsOver == false)
                    oneShots[i].Advance(deltaTime);
            }

            IReadOnlyList<CameraShakeContinuous> continuousShakes = cameraShakeModel.ContinuousShakes;
            for (int i = continuousShakes.Count - 1; i >= 0; i--) {
                if (continuousShakes[i].IsStopped)
                    cameraShakeModel.RemoveContinuousAt(i);
                else
                    continuousShakes[i].Advance(deltaTime);
            }
        }

        public CameraShakePose Sample(CameraShakeModel cameraShakeModel) {
            CameraShakePose pose = default;
            IReadOnlyList<CameraShakeOneShot> oneShots = cameraShakeModel.OneShots;
            for (int i = 0; i < oneShots.Count; i++) {
                CameraShakeOneShot shake = oneShots[i];
                pose = pose.Add(SampleShake(shake.Profile, shake.CurrentStrength, shake.NoiseTime));
            }

            IReadOnlyList<CameraShakeContinuous> continuousShakes = cameraShakeModel.ContinuousShakes;
            for (int i = 0; i < continuousShakes.Count; i++) {
                CameraShakeContinuous shake = continuousShakes[i];
                pose = pose.Add(SampleShake(shake.Profile, shake.Intensity, shake.NoiseTime));
            }

            return pose;
        }

        private static CameraShakePose SampleShake(CameraShakeProfile profile, float strength, float noiseTime) {
            float degrees = profile.Degrees * strength;
            float meters = profile.Meters * strength;
            Vector3 eulerDegrees = new(
                Noise(PITCH_SEED, noiseTime) * degrees,
                Noise(YAW_SEED, noiseTime) * degrees,
                Noise(ROLL_SEED, noiseTime) * degrees * profile.RollShare);
            Vector3 offset = new(Noise(OFFSET_X_SEED, noiseTime) * meters, Noise(OFFSET_Y_SEED, noiseTime) * meters, 0f);
            return new CameraShakePose(eulerDegrees, offset);
        }

        private static float Noise(float seed, float noiseTime) =>
            Mathf.PerlinNoise(seed, noiseTime) * 2f - 1f;
    }
}
