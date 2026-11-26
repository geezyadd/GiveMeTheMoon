using UnityEngine;

namespace Features.CharacterMovableModule.Scripts {
    // A takeoff spread over a short push of the legs instead of one step: the body gains the takeoff speed evenly over
    // the push time, so it starts like a jump, not like a shot. Zero push time gives the whole speed in one step.
    public sealed class JumpPush {
        private float _speedLeft;
        private float _secondsLeft;

        public bool IsPushing => _speedLeft > 0f;

        public void Begin(float speedChange, float pushSeconds) {
            _speedLeft = Mathf.Max(speedChange, 0f);
            _secondsLeft = Mathf.Max(pushSeconds, 0f);
        }

        // The upward speed to add on this step; the last step of the push takes whatever is left.
        public float Step(float deltaTime) {
            if (IsPushing == false)
                return 0f;

            float speed = _secondsLeft <= deltaTime ? _speedLeft : _speedLeft * (deltaTime / _secondsLeft);
            _speedLeft -= speed;
            _secondsLeft = Mathf.Max(_secondsLeft - deltaTime, 0f);
            if (_secondsLeft <= 0f)
                _speedLeft = 0f;
            return speed;
        }

        public void Stop() {
            _speedLeft = 0f;
            _secondsLeft = 0f;
        }
    }
}
