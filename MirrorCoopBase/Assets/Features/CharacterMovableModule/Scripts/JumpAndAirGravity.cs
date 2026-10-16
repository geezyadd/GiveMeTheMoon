using UnityEngine;

namespace Features.CharacterMovableModule.Scripts {
    // Jump timing (coyote time, jump buffer, cooldown, air jumps) and the extra air gravity of one player. The station
    // body (CharacterMovableBase) and the ship ride run the same rules, so a jump feels the same on both.
    public sealed class JumpAndAirGravity {
        private const float RISING_SPEED = 0.15f;
        private const float FALLING_SPEED = -0.15f;
        private const float LANDED_SPEED = 0.05f;
        private const float NEVER_SECONDS = 999f;

        private bool _jumpHeld;
        private bool _jumpQueued;
        private float _jumpReadyIn;
        private float _jumpPressedAgo = NEVER_SECONDS;
        private float _airTime = NEVER_SECONDS;
        private bool _jumpedThisAir;
        private int _airJumpsLeft;

        public void RefillAirJumps(int extraAirJumps) =>
            _airJumpsLeft = extraAirJumps;

        // Once per rendered frame.
        public void ReadInput(bool holdingJump, bool supported, float dt) {
            _jumpPressedAgo += dt;
            _airTime = supported ? 0f : _airTime + dt;
            if (holdingJump && _jumpHeld == false) {
                _jumpQueued = true;
                _jumpPressedAgo = 0f;
            }

            _jumpHeld = holdingJump;
        }

        // A press that must not turn into a jump later (it stood the player up from a seat).
        public void DropQueuedJump() =>
            _jumpQueued = false;

        // Once per fixed step. On a takeoff returns the vertical speed change; the caller also pauses its support.
        public bool TryTakeoff(in JumpTuning tuning, bool supported, float verticalSpeed, float gravity, float dt,
            out float speedChange) {
            speedChange = 0f;
            _jumpReadyIn -= dt;
            bool fromLedge = _airTime <= tuning.CoyoteTime && _jumpedThisAir == false;
            bool buffered = _jumpPressedAgo <= tuning.JumpBuffer;
            bool canGroundJump = (supported || fromLedge) && _jumpReadyIn <= 0f;
            bool canAirJump = _airJumpsLeft > 0 && _jumpReadyIn <= 0f;
            bool canJump = canGroundJump || canAirJump;
            bool takeoff = _jumpQueued && buffered && canJump;
            if (takeoff == false)
                return false;

            bool groundedTakeoff = supported || fromLedge;
            if (groundedTakeoff == false)
                _airJumpsLeft--;

            _jumpedThisAir = true;
            _jumpQueued = false;
            _jumpPressedAgo = tuning.JumpBuffer;
            _jumpReadyIn = tuning.JumpCooldown;
            float takeoffSpeed = Mathf.Sqrt(2f * tuning.JumpHeight * -gravity);
            speedChange = groundedTakeoff ? takeoffSpeed - verticalSpeed : takeoffSpeed;
            return true;
        }

        // Once per fixed step after TryTakeoff, with the vertical speed from before the step.
        public void Land(in JumpTuning tuning, bool supported, float verticalSpeed) {
            if (supported == false || _jumpedThisAir == false || verticalSpeed > LANDED_SPEED)
                return;

            _jumpedThisAir = false;
            _airJumpsLeft = tuning.ExtraAirJumps;
        }

        // Extra gravity in the air, as a multiple of the normal gravity.
        public float AirGravityExtra(in JumpTuning tuning, float verticalSpeed) {
            if (verticalSpeed > RISING_SPEED)
                return _jumpHeld ? tuning.RiseGravity : tuning.ShortHopGravity;

            if (verticalSpeed < FALLING_SPEED)
                return tuning.FallGravity;

            return 0f;
        }
    }
}
