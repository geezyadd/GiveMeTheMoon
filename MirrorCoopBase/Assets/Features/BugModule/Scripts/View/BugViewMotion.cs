using UnityEngine;

namespace Features.BugModule.Scripts.View {
    // The procedural motion of one bug's look, from the time, the bug's own phase and whether it sits on the ship:
    // beating or folded wings, dangling or scrabbling legs, the tail base's twitch (Perlin, so it jerks rather than
    // swings), the gnawing nod and sway, and the squash of a flinch.
    public sealed class BugViewMotion {
        private const float FULL_TURN = Mathf.PI * 2f;
        private const float LEG_PHASE_STEP = 0.5f;
        private const float NOISE_CENTER = 0.5f;
        private const float NOISE_SCALE = 2f;
        private const float SECOND_AXIS_OFFSET = 17.3f;

        private readonly BugViewFeel _feel;

        public BugViewMotion(BugViewFeel feel) =>
            _feel = feel;

        public float CalculateWingDegrees(bool isSitting, float time, float phase) {
            if (isSitting)
                return _feel.WingFoldDegrees + Wave(time * _feel.WingBeatsPerSecond * 2f + phase) * _feel.WingBuzzDegrees;

            return Wave(time * _feel.WingBeatsPerSecond + phase) * _feel.WingBeatDegrees;
        }

        // Neighbouring legs swing against each other.
        public float CalculateLegDegrees(bool isSitting, float time, float phase, int legIndex) {
            float swingsPerSecond = isSitting ? _feel.DeckLegSwingsPerSecond : _feel.FlightLegSwingsPerSecond;
            return Wave(time * swingsPerSecond + phase + legIndex * LEG_PHASE_STEP) * _feel.LegSwingDegrees;
        }

        // Yaw and pitch of the tail base, degrees.
        public Vector2 CalculateTailTwitch(bool isSitting, float time, float phase) {
            float degrees = isSitting ? _feel.GnawTailTwitchDegrees : _feel.FlightTailTwitchDegrees;
            float noiseTime = time * _feel.TailTwitchFrequency;
            float yaw = (Mathf.PerlinNoise(noiseTime, phase) - NOISE_CENTER) * NOISE_SCALE;
            float pitch = (Mathf.PerlinNoise(phase + SECOND_AXIS_OFFSET, noiseTime) - NOISE_CENTER) * NOISE_SCALE;
            return new Vector2(yaw, pitch) * degrees;
        }

        // The whole body's nod (x) and sway (z) while it gnaws; none in flight (the field agent feel banks it there).
        public Vector3 CalculateGnawEuler(bool isSitting, float time, float phase) {
            if (isSitting == false)
                return Vector3.zero;

            float nod = Mathf.Max(0f, Wave(time * _feel.GnawNodsPerSecond + phase)) * _feel.GnawNodDegrees;
            float sway = Wave(time * _feel.GnawSwaysPerSecond + phase) * _feel.GnawSwayDegrees;
            return new Vector3(nod, 0f, sway);
        }

        // Squashed flat at the strike and back to full size as the flinch ends.
        public Vector3 CalculateFlinchScale(float flinchSecondsLeft) {
            float squash = _feel.FlinchSquash * Mathf.Clamp01(flinchSecondsLeft / _feel.FlinchSeconds);
            return new Vector3(1f + squash, 1f - squash, 1f + squash);
        }

        private static float Wave(float cycles) =>
            Mathf.Sin(cycles * FULL_TURN);
    }
}
