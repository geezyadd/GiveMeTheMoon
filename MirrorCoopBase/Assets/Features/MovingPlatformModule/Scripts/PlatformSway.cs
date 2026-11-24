using UnityEngine;

namespace Features.MovingPlatformModule {
    // A slow rocking of a platform, like a deck leaning into turns: a roll from side to side and a smaller pitch, each a
    // sine of the server time, the pitch a quarter period behind the roll so the tilt goes round instead of in a line.
    public static class PlatformSway {
        private const float FULL_TURN = Mathf.PI * 2f;

        // The tilt to put on top of the platform's heading, in the platform's own axes.
        public static Quaternion TiltAt(double time, float rollDegrees, float pitchDegrees, float periodSeconds) {
            if (periodSeconds <= 0f)
                return Quaternion.identity;

            float phase = (float)(time % periodSeconds / periodSeconds) * FULL_TURN;
            float roll = Mathf.Sin(phase) * rollDegrees;
            float pitch = Mathf.Cos(phase) * pitchDegrees;
            return Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
