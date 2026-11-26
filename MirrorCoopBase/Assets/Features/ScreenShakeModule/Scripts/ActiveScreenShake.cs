namespace Features.ScreenShakeModule {
    // One shake running: its profile, the strength it was asked with and the time it has left; its amplitude fades
    // linearly to zero over the profile's duration.
    public sealed class ActiveScreenShake {
        public ActiveScreenShake(ScreenShakeProfile profile, float strength) {
            Profile = profile;
            Strength = strength;
            TimeLeft = profile.Duration;
        }

        public ScreenShakeProfile Profile { get; }
        public float Strength { get; }
        public float TimeLeft { get; private set; }
        public float Amplitude => Profile.Amplitude * Strength * TimeLeft / Profile.Duration;

        public void SetTimeLeft(float timeLeft) =>
            TimeLeft = timeLeft;
    }
}
