using Features.CharacterMovableModule.Scripts.PlayerStats;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.MovementControls {
    // The player's own controls: the keyboard from CharacterInputBuffer, turned by this peer's camera when the stats
    // say walking is camera-relative. A jump press is the jump key going down since the last frame.
    public sealed class KeyboardMovementControls : IMovementControls {
        private readonly CharacterInputBuffer _input;
        private readonly PlayerStatEntity _stats;
        private bool _wasJumpHeld;

        public KeyboardMovementControls(CharacterInputBuffer input, PlayerStatEntity stats) {
            _input = input;
            _stats = stats;
        }

        public bool SprintHeld => _input.SprintHeld;
        public bool JumpHeld => _input.JumpHeld;

        public Vector3 ReadWish() {
            Vector2 stick = _input.MoveStick;
            return _stats.CameraRelative
                ? CameraPlanar(stick)
                : new Vector3(stick.x, 0f, stick.y);
        }

        public bool ConsumeJumpPress() {
            bool held = _input.JumpHeld;
            bool pressed = held && _wasJumpHeld == false;
            _wasJumpHeld = held;
            return pressed;
        }

        private static Vector3 CameraPlanar(Vector2 stick) {
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null || stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
                return new Vector3(stick.x, 0f, stick.y);

            Vector3 world = forward.normalized * stick.y + right.normalized * stick.x;
            if (world.sqrMagnitude > 1f)
                world.Normalize();

            return world;
        }
    }
}
