using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRider : NetworkBehaviour {
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private FloatingController _floating;
        [SerializeField] private NetworkRigidbodyUnreliable _netBody;
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8f;
        [SerializeField] private float _jumpHeight = 4f;
        [SerializeField] private float _deckInset = 0.35f;

        [SyncVar(hook = nameof(OnSyncedOffsetChanged))]
        private Vector3 _syncedLocalOffset;

        [SyncVar(hook = nameof(OnRidingChanged))]
        private bool _syncedRiding;

        [SyncVar(hook = nameof(OnSeatedChanged))]
        private bool _seated;

        [SyncVar]
        private bool _helmSeat;

        [Inject]
        private CharacterInputBuffer _input;

        [Inject]
        private IGameCameraService _cameras;

        private const float OffsetSendSeconds = 0.05f;

        private bool _bound;
        private Vector3 _localOffset;
        private Vector3 _offsetFrom;
        private Vector3 _offsetTo;
        private float _offsetBlend = 1f;
        private bool _jumpHeldPrev;
        private Vector3 _lastPlatformPos;
        private ShipBase _ship;
        private Transform _platform;
        private bool _wasKinematic;
        private bool _netBodyWasEnabled;
        private float _nextOffsetSend;
        private float _nextSteerSend;
        private float _rideRestY;
        private float _rideJumpVel;

        public bool IsRiding => _bound;
        public bool IsSeated => _seated;
        internal bool IsHelmSeat => _helmSeat;
        internal float CurrentSteer => _input != null ? _input.MoveStick.x : 0f;

        internal bool WantsLand(ShipBase ship) {
            if (isOwned == false || _bound || _rb == null || ship == null)
                return false;

            Transform platform = ship.transform;
            Vector3 local = Quaternion.Inverse(platform.rotation) * (transform.position - platform.position);
            if (ship.ContainsDeckWalk(local, _deckInset) == false)
                return false;

            if (_rb.linearVelocity.y > 0.15f)
                return false;

            return local.y <= 1.75f && local.y >= 0.25f;
        }

        internal void BindToPlatform(ShipBase ship) {
            if (_rb == null || ship == null || _bound)
                return;

            _ship = ship;
            _platform = ship.transform;
            _localOffset = Quaternion.Inverse(_platform.rotation) * (transform.position - _platform.position);
            _ship.ClampDeckWalk(ref _localOffset, _deckInset);
            _rideRestY = _localOffset.y;
            _rideJumpVel = 0f;
            _offsetFrom = _localOffset;
            _offsetTo = _localOffset;
            _offsetBlend = 1f;
            _lastPlatformPos = _platform.position;
            _bound = true;
            _wasKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            if (_floating != null)
                _floating.HoverEnabled = false;

            if (isOwned) {
                _syncedLocalOffset = _localOffset;
                CmdSetRideOffset(_localOffset);
                CmdSetRiding(true);
            }

            SilenceNetworkBody();
        }

        internal void ReleaseFromPlatform() {
            if (_bound == false)
                return;

            _bound = false;
            _platform = null;
            _seated = false;
            _helmSeat = false;

            if (_rb != null) {
                _rb.isKinematic = _wasKinematic;
                _rb.detectCollisions = true;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            if (_floating != null)
                _floating.HoverEnabled = true;

            RestoreNetworkBody();
        }

        internal void ServerLockSeat(Vector3 localOffset, bool helm) {
            _seated = true;
            _helmSeat = helm;
            _localOffset = localOffset;
            _syncedLocalOffset = localOffset;
        }

        internal void ServerUnlockSeat() {
            _seated = false;
            _helmSeat = false;
        }

        internal void Follow(float dt) {
            if (_bound == false || _platform == null)
                return;

            if (isOwned) {
                bool jumped = ConsumeJumpPress();
                if (_seated) {
                    if (_helmSeat)
                        SendSteerIfNeeded();

                    if (jumped) {
                        CmdStand();
                        return;
                    }
                }
                else {
                    ApplyOwnedWalk(dt);
                    if (jumped)
                        BeginRideJump();

                    SimulateRideJump(dt);
                }
            }

            Vector3 next = _platform.position + _platform.rotation * RideOffset();
            transform.position = next;
            if (_rb != null)
                _rb.position = next;

            _lastPlatformPos = _platform.position;
        }

        private bool ConsumeJumpPress() {
            bool pressed = _input != null && _input.JumpHeld && _jumpHeldPrev == false;
            _jumpHeldPrev = _input != null && _input.JumpHeld;
            return pressed;
        }

        private void ApplyOwnedWalk(float dt) {
            if (_input == null || dt <= 0f)
                return;

            Vector3 worldMove = CameraPlanar(_input.MoveStick) * CurrentSpeed() * dt;
            Vector3 localMove = Quaternion.Inverse(_platform.rotation) * worldMove;
            _localOffset.x += localMove.x;
            _localOffset.z += localMove.z;
            if (_ship != null)
                _ship.ClampDeckWalk(ref _localOffset, _deckInset);

            SendOffsetIfNeeded();
        }

        private void BeginRideJump() {
            if (_localOffset.y > _rideRestY + 0.05f)
                return;

            _rideJumpVel = Mathf.Sqrt(2f * -Physics.gravity.y * Mathf.Max(0.1f, _jumpHeight));
            _nextOffsetSend = 0f;
            SendOffsetIfNeeded();
        }

        private void SimulateRideJump(float dt) {
            if (dt <= 0f)
                return;

            bool grounded = _localOffset.y <= _rideRestY + 0.001f && _rideJumpVel <= 0f;
            if (grounded) {
                _localOffset.y = _rideRestY;
                _rideJumpVel = 0f;
                return;
            }

            float extra = 0f;
            if (_rideJumpVel > 0.15f)
                extra = _input != null && _input.JumpHeld ? 1.2f : 2f;
            else if (_rideJumpVel < -0.15f)
                extra = 3f;

            _rideJumpVel += Physics.gravity.y * (1f + extra) * dt;
            _localOffset.y += _rideJumpVel * dt;
            if (_localOffset.y > _rideRestY)
                return;

            _localOffset.y = _rideRestY;
            _rideJumpVel = 0f;
            SendOffsetIfNeeded();
        }

        private void SendOffsetIfNeeded() {
            if (Time.unscaledTime < _nextOffsetSend)
                return;

            _nextOffsetSend = Time.unscaledTime + OffsetSendSeconds;
            if ((_syncedLocalOffset - _localOffset).sqrMagnitude > 0.0004f)
                CmdSetRideOffset(_localOffset);
        }

        private void SendSteerIfNeeded() {
            if (_input == null || _ship == null)
                return;

            if (isServer) {
                _ship.ServerSetSteer(netId, _input.MoveStick.x);
                return;
            }

            if (Time.unscaledTime < _nextSteerSend)
                return;

            _nextSteerSend = Time.unscaledTime + 0.03f;
            CmdSetSteer(_input.MoveStick.x);
        }

        private Vector3 RideOffset() {
            if (isOwned || _seated)
                return _localOffset;

            _offsetBlend = Mathf.Min(_offsetBlend + Time.deltaTime / OffsetSendSeconds, 1f);
            return Vector3.Lerp(_offsetFrom, _offsetTo, _offsetBlend);
        }

        private void OnSyncedOffsetChanged(Vector3 previous, Vector3 current) {
            if (isOwned)
                return;

            _offsetFrom = Vector3.Lerp(_offsetFrom, _offsetTo, _offsetBlend);
            _offsetTo = current;
            _localOffset = current;
            _offsetBlend = 0f;
        }

        private void OnSeatedChanged(bool previous, bool current) {
            if (current)
                _localOffset = _syncedLocalOffset;
        }

        private void OnRidingChanged(bool previous, bool current) {
            if (isOwned)
                return;

            if (current == false && _bound)
                ReleaseFromPlatform();

            if (current && _bound == false && _ship != null)
                BindToPlatform(_ship);
        }

        private float CurrentSpeed() {
            if (_input != null && _input.SprintHeld)
                return _sprintSpeed;

            return _walkSpeed;
        }

        private Vector3 CameraPlanar(Vector2 stick) {
            if (stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Transform cameraTransform = _cameras != null && _cameras.OutputCamera != null
                ? _cameras.OutputCamera.transform
                : null;
            if (cameraTransform == null)
                return new Vector3(stick.x, 0f, stick.y);

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

        [Command]
        private void CmdSetRideOffset(Vector3 local) {
            _syncedLocalOffset = local;
        }

        [Command]
        private void CmdSetRiding(bool riding) {
            _syncedRiding = riding;
        }

        [Command]
        private void CmdSetSteer(float lateral) {
            if (_ship != null)
                _ship.ServerSetSteer(netId, lateral);
        }

        [Command]
        private void CmdStand() {
            if (_ship != null)
                _ship.ServerStand(this);
        }

        private void SilenceNetworkBody() {
            if (_netBody == null)
                return;

            _netBodyWasEnabled = _netBody.enabled;
            _netBody.enabled = false;
        }

        private void RestoreNetworkBody() {
            if (_netBody == null)
                return;

            _netBody.enabled = _netBodyWasEnabled;
        }
    }
}
