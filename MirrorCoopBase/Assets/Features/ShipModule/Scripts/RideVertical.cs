using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The vertical motion of a rider in ship space: the same hover spring, jump and air gravity a player has on the
    // station, integrated in fixed steps like the physics step, so the deck feels the same at any frame rate.
    public sealed class RideVertical {
        private const int MAX_STEPS_PER_FRAME = 8;

        private readonly JumpAndAirGravity _jump = new JumpAndAirGravity();
        private float _previousHeight;
        private float _height;
        private float _velocity;
        private float _output;
        private float _carry;
        private float _supportPauseLeft;
        private bool _supported;

        // Starts from a height set from outside (bind, seat, debug), with the given vertical speed.
        public void Reset(float height, float velocity, bool jumpHeld, int extraAirJumps) {
            _previousHeight = height;
            _height = height;
            _output = height;
            _velocity = velocity;
            _carry = 0f;
            _supportPauseLeft = 0f;
            _supported = false;
            _jump.ReadInput(jumpHeld, false, 0f);
            _jump.DropQueuedJump();
            _jump.RefillAirJumps(extraAirJumps);
        }

        public void DropQueuedJump() =>
            _jump.DropQueuedJump();

        public void ReadInput(bool jumpHeld, float dt) =>
            _jump.ReadInput(jumpHeld, _supported, dt);

        // Advances by one rendered frame and returns the height to show, interpolated between the last two steps.
        // height is the current height in ship space; one that differs from the last output was moved from outside.
        public float Advance(float height, float dt, in RideVerticalFrame frame) {
            if (Mathf.Approximately(height, _output) == false) {
                _previousHeight = height;
                _height = height;
                _carry = 0f;
            }

            float step = frame.Step;
            _carry = Mathf.Min(_carry + dt, step * MAX_STEPS_PER_FRAME);
            while (_carry >= step) {
                _carry -= step;
                _previousHeight = _height;
                Simulate(frame);
            }

            _output = Mathf.Lerp(_previousHeight, _height, _carry / step);
            return _output;
        }

        // One physics-like step, in the order the station runs it: the float samples its support and pushes, the body
        // jumps and adds the air gravity, then velocity and position are integrated (semi-implicit Euler).
        private void Simulate(in RideVerticalFrame frame) {
            float step = frame.Step;
            HoverSpringSettings spring = frame.Spring;
            float supportDistance = spring.Clearance - (frame.RestHeight - _height);
            if (_supportPauseLeft > 0f) {
                _supportPauseLeft -= step;
                _supported = false;
            }
            else {
                _supported = HoverSpring.Reaches(spring, supportDistance);
            }

            float speedBefore = _velocity;
            float acceleration = frame.Gravity;
            if (_supported && HoverSpring.Holds(spring, speedBefore))
                acceleration += HoverSpring.Acceleration(spring, supportDistance, speedBefore) - frame.Gravity;

            bool supported = _supported;
            float speedChange = 0f;
            if (_jump.TryTakeoff(frame.Jump, supported, speedBefore, frame.Gravity, step, out float takeoff)) {
                speedChange = takeoff;
                _supportPauseLeft = spring.SupportPause;
                _supported = false;
            }

            _jump.Land(frame.Jump, supported, speedBefore);
            if (supported == false)
                acceleration += frame.Gravity * _jump.AirGravityExtra(frame.Jump, speedBefore);

            _velocity += acceleration * step + speedChange;
            _height += _velocity * step;
            // The body collider lands on the deck: it never sinks below it, whatever the frame rate.
            if (_height >= frame.FloorHeight)
                return;

            _height = frame.FloorHeight;
            if (_velocity < 0f)
                _velocity = 0f;
        }
    }
}
