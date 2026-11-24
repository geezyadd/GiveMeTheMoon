using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.ShipModule.Scripts.Riding {
    // How the owner of a bound rider moves on the deck in the ship's axes: walking and sprinting from the keyboard along
    // the camera, flattened on the deck plane, and the vertical (the station's hover spring, jump and air gravity) worked
    // out in ship space from the player's own float and stats.
    internal sealed class ShipDeckWalk {
        private readonly FloatingController _floating;
        private readonly PlayerStatEntity _stats;
        private readonly float _walkSpeed;
        private readonly float _sprintSpeed;
        private readonly float _deckInset;
        private readonly RideVertical _vertical = new RideVertical();

        private CharacterInputBuffer _input;
        private IGameCameraService _cameras;
        private bool _jumpHeldPrev;
        private float _rideRestY;
        private float _rideFloorY;
        private Quaternion _lastPlatformRot = Quaternion.identity;

        public ShipDeckWalk(FloatingController floating, PlayerStatEntity stats, float walkSpeed, float sprintSpeed, float deckInset) {
            _floating = floating;
            _stats = stats;
            _walkSpeed = walkSpeed;
            _sprintSpeed = sprintSpeed;
            _deckInset = deckInset;
        }

        public void SetInput(CharacterInputBuffer input, IGameCameraService cameras) {
            _input = input;
            _cameras = cameras;
        }

        // On binding: the rest and floor heights over the deck at this offset, and the vertical starting from it.
        public void Begin(IShipDeckGeometry deckGeometry, Vector3 localOffset, float verticalSpeed, bool isOwned) {
            _rideRestY = ResolveRideRestY(deckGeometry, localOffset);
            _rideFloorY = ResolveRideFloorY();
            _jumpHeldPrev = _input.JumpHeld;
            Reset(localOffset.y, isOwned && localOffset.y > _rideRestY ? verticalSpeed : 0f);
        }

        public void Reset(float height, float velocity) =>
            _vertical.Reset(
                height,
                velocity,
                _input.JumpHeld,
                JumpTuning.Read(_stats).ExtraAirJumps);

        // The ship's rotation the camera was last presented with: the walk direction is turned into the deck by it.
        public void TrackPlatformRotation(Quaternion rotation) =>
            _lastPlatformRot = rotation;

        public bool ConsumeJumpPress() {
            bool pressed = _input != null && _input.JumpHeld && _jumpHeldPrev == false;
            _jumpHeldPrev = _input != null && _input.JumpHeld;
            return pressed;
        }

        public void ReadVerticalInput(float dt) =>
            _vertical.ReadInput(_input.JumpHeld, dt);

        // The press that stands a seated rider up must not jump right after.
        public void DropQueuedJump() =>
            _vertical.DropQueuedJump();

        // Moves the offset by this frame's walk; false when there is no input or no time, and nothing moved.
        public bool Walk(ref Vector3 localOffset, ShipBase ship, float dt) {
            if (_input == null || dt <= 0f)
                return false;

            Vector3 localMove = DeckPlanar(_input.MoveStick) * CurrentSpeed() * dt;
            localOffset.x += localMove.x;
            localOffset.z += localMove.z;
            if (ship.ConfinesRidersToDeck || ship.IsFlying == false)
                ship.DeckGeometry.ClampDeckWalk(ref localOffset, _deckInset);

            return true;
        }

        // The station hover spring, jump and air gravity in ship space, from this player's own float and stats.
        public void SimulateVertical(ref Vector3 localOffset, float dt) {
            if (dt <= 0f)
                return;

            RideVerticalFrame frame = new RideVerticalFrame(
                _floating.Spring,
                JumpTuning.Read(_stats),
                _rideRestY,
                _rideFloorY,
                Physics.gravity.y,
                Time.fixedDeltaTime);
            localOffset.y = _vertical.Advance(localOffset.y, dt, frame);
        }

        private float ResolveRideRestY(IShipDeckGeometry deckGeometry, Vector3 localOffset) {
            if (_floating == null || deckGeometry.TryGetDeckSurfaceY(localOffset, out float deckSurfaceY) == false)
                return localOffset.y;

            return deckSurfaceY + _floating.StandHeight;
        }

        // The rider collider stands on the deck: the spring may sag the rider down to here, never lower.
        private float ResolveRideFloorY() =>
            _rideRestY - _floating.StandHeight + _floating.ContactHeight;

        private float CurrentSpeed() {
            if (_input != null && _input.SprintHeld)
                return _sprintSpeed;

            return _walkSpeed;
        }

        // Walk direction in ship space, flattened on the deck plane: flattening on the world horizontal bends the
        // walk while the ship banks. The camera was presented last frame with the ship at _lastPlatformRot.
        private Vector3 DeckPlanar(Vector2 stick) {
            if (stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Transform cameraTransform = _cameras != null && _cameras.OutputCamera != null
                ? _cameras.OutputCamera.transform
                : null;
            if (cameraTransform == null)
                return new Vector3(stick.x, 0f, stick.y);

            Quaternion toDeck = Quaternion.Inverse(_lastPlatformRot);
            Vector3 forward = toDeck * cameraTransform.forward;
            Vector3 right = toDeck * cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
                return new Vector3(stick.x, 0f, stick.y);

            Vector3 local = forward.normalized * stick.y + right.normalized * stick.x;
            if (local.sqrMagnitude > 1f)
                local.Normalize();

            return local;
        }
    }
}
