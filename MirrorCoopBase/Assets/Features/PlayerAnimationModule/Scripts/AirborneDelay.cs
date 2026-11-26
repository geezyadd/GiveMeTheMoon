namespace Features.PlayerAnimationModule {
    // Whether a body counts as flying: only after it has been off the ground for the whole delay, so the slightest lift
    // (a step, a bump, a flicker of the ground sensor) does not. Back on the ground it stops counting at once.
    public sealed class AirborneDelay {
        private float _secondsInAir;

        public bool Step(bool isInAir, float delaySeconds, float deltaTime) {
            if (isInAir == false) {
                _secondsInAir = 0f;
                return false;
            }

            _secondsInAir += deltaTime;
            return _secondsInAir >= delaySeconds;
        }

        public void Reset() =>
            _secondsInAir = 0f;
    }
}
