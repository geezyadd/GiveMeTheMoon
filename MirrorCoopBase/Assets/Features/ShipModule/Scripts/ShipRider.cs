using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Debug;
#endif
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
        [SerializeField] private PlayerStatEntity _stats;
        [SerializeField] private float _deckInset = 0.08f;

        // Mirror runs hooks in declaration order: the offset hook sees its fresh stamp, a bind sees the fresh offset.
        [SyncVar]
        private double _syncedOffsetTime;

        [SyncVar(hook = nameof(OnSyncedOffsetChanged))]
        private Vector3 _syncedLocalOffset;

        // The ship this rider is bound to (its pose-sync identity), null when not riding. Every peer that does not own
        // the rider binds it from this, at the owner's ship-local offset.
        [SyncVar(hook = nameof(OnRideShipChanged))]
        private NetworkIdentity _syncedRideShip;

        [SyncVar(hook = nameof(OnSeatedChanged))]
        private bool _seated;

        [SyncVar]
        private bool _helmSeat;

        [Inject]
        private CharacterInputBuffer _input;

        [Inject]
        private IGameCameraService _cameras;

        private const float OffsetSendSeconds = 0.05f;
        private const float OFFSET_RESEND_SQR_METERS = 0.000001f;
        private const float MAX_RIDE_SPEED = 120f;
        private const double REMOTE_OFFSET_DELAY = 2d * OffsetSendSeconds;

        private bool _bound;
        private Vector3 _localOffset;
        private readonly RideOffsetBuffer _remoteOffsets = new RideOffsetBuffer(OffsetSendSeconds);
        private bool _jumpHeldPrev;
        private Vector3 _lastPlatformPos;
        private Quaternion _lastPlatformRot = Quaternion.identity;
        private ShipBase _ship;
        private Transform _platform;
        private bool _wasKinematic;
        private RigidbodyInterpolation _wasInterpolation;
        private bool _netBodyWasEnabled;
        private float _nextOffsetSend;
        private float _nextSteerSend;
        private float _rideRestY;
        private float _rideFloorY;
        private readonly RideVertical _vertical = new RideVertical();
        private Vector3 _rideVelocity;
        private float _reboardAt;

        internal Vector3 DebugLocalOffset => _localOffset;
        public bool IsRiding => _bound;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal ShipRideDiagnostics RideDiagnostics { get; } = new ShipRideDiagnostics();
#endif
        public bool IsSeated => _seated;
        internal bool IsHelmSeat => _helmSeat;
        internal float CurrentSteer => _input != null ? _input.MoveStick.x : 0f;

        public override void OnStopServer() {
            if (_ship != null)
                _ship.ServerRemoveRider(this);
        }

        internal ShipRideRefusal CheckLanding(ShipBase ship) {
            // Right after a drop the interpolated body still trails the moving deck by a frame; without this it lands
            // back on the deck it just left.
            if (Time.time < _reboardAt)
                return ShipRideRefusal.ReboardDelay;

            return ship.RideRule.CheckCatch(ship.ToShipLocal(transform.position), _rb.linearVelocity.y, _deckInset);
        }

        internal void BindToPlatform(ShipBase ship) {
            if (_rb == null || ship == null || _bound)
                return;

            Vector3 standingOffset = ship.ToShipLocal(transform.position);
            ship.DeckGeometry.ClampDeckWalk(ref standingOffset, _deckInset);
            // A seated rider keeps the server's seat offset; its own standing spot would overwrite it on the server.
            Bind(ship, _seated ? _syncedLocalOffset : standingOffset);
        }

        internal void BindToSeat(ShipBase ship, Vector3 seatOffset) {
            if (_bound) {
                _localOffset = seatOffset;
                // A rider who sat down mid-jump would get the rest of the jump back when standing up.
                ResetVertical(0f);
                return;
            }

            if (_rb != null && ship != null) {
                Bind(ship, seatOffset);
                ResetVertical(0f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RideDiagnostics.LogBound(this, ship, ShipRideDiagnostics.CAUSE_SEAT);
#endif
            }
        }

        private void Bind(ShipBase ship, Vector3 localOffset) {
            _ship = ship;
            _platform = ship.transform;
            Quaternion toLocal = Quaternion.Inverse(_platform.rotation);
            float localVerticalSpeed = (toLocal * _rb.linearVelocity).y;
            _localOffset = localOffset;
            _rideRestY = ResolveRideRestY();
            _rideFloorY = ResolveRideFloorY();
            _jumpHeldPrev = _input != null && _input.JumpHeld;
            ResetVertical(isOwned && _localOffset.y > _rideRestY ? localVerticalSpeed : 0f);
            _remoteOffsets.Reset(_syncedOffsetTime, _localOffset);
            _lastPlatformPos = _platform.position;
            _lastPlatformRot = _platform.rotation;
            _bound = true;
            _wasKinematic = _rb.isKinematic;
            _wasInterpolation = _rb.interpolation;
            _rb.StopIfDynamic();
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            if (transform.parent != null)
                transform.SetParent(null, true);

            ApplyWorldPose();

            if (_floating != null)
                _floating.HoverEnabled = false;

            if (isOwned) {
                _syncedLocalOffset = _localOffset;
                CmdBeginRide(_localOffset, NetworkTime.time, ship.NetIdentity);
            }

            SilenceNetworkBody();
        }

        private void BindRemote(ShipBase ship) {
            ship.Riders.TrackRider(this);
            if (_bound == false) {
                Bind(ship, _syncedLocalOffset);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RideDiagnostics.LogBound(this, ship, ShipRideDiagnostics.CAUSE_OWNER_RIDE);
#endif
                return;
            }

            SnapToSyncedOffset();
        }

        internal void DebugSetLocalOffset(Vector3 localOffset) {
            _localOffset = localOffset;
            ResetVertical(0f);
            _remoteOffsets.Reset(NetworkTime.time, localOffset);
            if (_platform == null)
                return;

            ApplyWorldPose();
        }

        // Server-authoritative release (landing, wreck, stand up at a station, walking off the deck): every peer releases
        // this rider, the owner with its ride velocity.
        [Server]
        internal void ServerRelease() {
            if (_bound == false && _syncedRideShip == null)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RideDiagnostics.LogReleased(this, _ship, ShipRideDiagnostics.CAUSE_SERVER_RELEASE);
#endif
            _seated = false;
            _helmSeat = false;
            _syncedRideShip = null;
            if (_bound)
                ReleaseFromPlatform();

            RpcRelease();
        }

        internal void ReleaseFromPlatform() {
            if (_bound == false)
                return;

            // The ship may have been snapped (origin shift at the station) since the last Follow.
            ApplyWorldPose();
            bool carryVelocity = isOwned && _ship.IsFlying;
            if (carryVelocity)
                _reboardAt = Time.time + _ship.ReboardDelaySeconds;
            _bound = false;
            Transform released = transform;
            Vector3 worldPosition = released.position;
            Quaternion worldRotation = released.rotation;
            released.SetParent(null, true);
            released.SetPositionAndRotation(worldPosition, worldRotation);
            _platform = null;
            if (isServer) {
                _seated = false;
                _helmSeat = false;
            }

            if (_rb != null) {
                _rb.isKinematic = _wasKinematic;
                _rb.detectCollisions = true;
                _rb.interpolation = _wasInterpolation;
                if (carryVelocity && _rb.isKinematic == false)
                    _rb.linearVelocity = _rideVelocity;
            }

            if (_floating != null)
                _floating.HoverEnabled = true;

            RestoreNetworkBody();
            if (isOwned && NetworkClient.ready)
                CmdEndRide();
        }

        internal void ServerLockSeat(Vector3 localOffset, bool helm, NetworkIdentity ship) {
            _seated = true;
            _helmSeat = helm;
            _localOffset = localOffset;
            _syncedOffsetTime = NetworkTime.time;
            _syncedLocalOffset = localOffset;
            _syncedRideShip = ship;
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
                _vertical.ReadInput(_input.JumpHeld, dt);
                if (_seated) {
                    // The press that stands the rider up must not jump right after.
                    _vertical.DropQueuedJump();
                    if (_helmSeat)
                        SendSteerIfNeeded();

                    if (jumped)
                        CmdStand();
                }
                else {
                    ApplyOwnedWalk(dt);
                    SimulateRideVertical(dt);
                    SendOffsetIfNeeded();
                }
            }

            Vector3 next = RideOffset();
            _localOffset = next;
            Vector3 previousWorld = transform.position;
            ApplyWorldPose();
            TrackRideVelocity(previousWorld, dt);
            _lastPlatformPos = _platform.position;
            _lastPlatformRot = _platform.rotation;
            if (IsOffDeckEdge())
                DropOffDeck();
        }

        private void TrackRideVelocity(Vector3 previousWorld, float dt) {
            if (dt <= 0f)
                return;

            Vector3 velocity = (transform.position - previousWorld) / dt;
            // An origin shift moves the ship hundreds of metres in one frame; that is not a ride velocity.
            if (velocity.sqrMagnitude <= MAX_RIDE_SPEED * MAX_RIDE_SPEED)
                _rideVelocity = velocity;
        }

        private bool IsOffDeckEdge() {
            if (_seated || _ship.IsFlying == false || _ship.ConfinesRidersToDeck)
                return false;

            if (isOwned)
                return _ship.DeckGeometry.ContainsDeckWalk(_localOffset, _deckInset) == false;

            return isServer && _ship.DeckGeometry.ContainsDeckWalk(_syncedLocalOffset, _deckInset) == false;
        }

        // The server decides; an owning client releases at once so it does not walk on air for a round trip, and the
        // server's own check covers a lost or late end-ride command.
        private void DropOffDeck() {
            if (isServer) {
                ServerRelease();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RideDiagnostics.LogReleased(this, _ship, ShipRideDiagnostics.CAUSE_DECK_EDGE);
#endif
            ReleaseFromPlatform();
        }

        private void SnapToSyncedOffset() {
            _localOffset = _syncedLocalOffset;
            _remoteOffsets.Reset(_syncedOffsetTime, _syncedLocalOffset);
        }

        private void ApplyWorldPose() {
            if (_platform == null)
                return;

            Vector3 world = _platform.TransformPoint(_localOffset);
            Quaternion rotation = _platform.rotation;
            transform.SetPositionAndRotation(world, rotation);
            if (_rb == null)
                return;

            _rb.StopIfDynamic();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.None;
            _rb.position = world;
            _rb.rotation = rotation;
        }

        private bool ConsumeJumpPress() {
            bool pressed = _input != null && _input.JumpHeld && _jumpHeldPrev == false;
            _jumpHeldPrev = _input != null && _input.JumpHeld;
            return pressed;
        }

        private void ApplyOwnedWalk(float dt) {
            if (_input == null || dt <= 0f)
                return;

            Vector3 localMove = DeckPlanar(_input.MoveStick) * CurrentSpeed() * dt;
            _localOffset.x += localMove.x;
            _localOffset.z += localMove.z;
            if (_ship.ConfinesRidersToDeck || _ship.IsFlying == false)
                _ship.DeckGeometry.ClampDeckWalk(ref _localOffset, _deckInset);

            SendOffsetIfNeeded();
        }

        // The station hover spring, jump and air gravity in ship space, from this player's own float and stats.
        private void SimulateRideVertical(float dt) {
            if (dt <= 0f)
                return;

            RideVerticalFrame frame = new RideVerticalFrame(
                _floating.Spring,
                JumpTuning.Read(_stats),
                _rideRestY,
                _rideFloorY,
                Physics.gravity.y,
                Time.fixedDeltaTime);
            _localOffset.y = _vertical.Advance(_localOffset.y, dt, frame);
        }

        private void ResetVertical(float velocity) =>
            _vertical.Reset(
                _localOffset.y,
                velocity,
                _input.JumpHeld,
                JumpTuning.Read(_stats).ExtraAirJumps);

        private void SendOffsetIfNeeded() {
            if (Time.unscaledTime < _nextOffsetSend)
                return;

            _nextOffsetSend = Time.unscaledTime + OffsetSendSeconds;
            if ((_syncedLocalOffset - _localOffset).sqrMagnitude > OFFSET_RESEND_SQR_METERS)
                CmdSetRideOffset(_localOffset, NetworkTime.time);
        }

        private void SendSteerIfNeeded() {
            if (_input == null || _ship == null)
                return;

            if (isServer) {
                _ship.Seats.ServerSetSteer(netId, _input.MoveStick.x);
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

            return _remoteOffsets.Sample(NetworkTime.time - REMOTE_OFFSET_DELAY);
        }

        private void OnSyncedOffsetChanged(Vector3 previous, Vector3 current) {
            if (isOwned)
                return;

            // A seated rider's offset only changes when it switches seats (_seated stays true): it moves there at once.
            if (_seated) {
                SnapToSyncedOffset();
                return;
            }

            _remoteOffsets.Add(_syncedOffsetTime, current);
        }

        // Standing up must not blend back to the offset from before the seat.
        private void OnSeatedChanged(bool previous, bool current) {
            if (isOwned && current == false) {
                // Standing up starts from rest on the seat, never with a velocity left over from before sitting.
                ResetVertical(0f);
                return;
            }

            SnapToSyncedOffset();
        }

        private void OnRideShipChanged(NetworkIdentity previous, NetworkIdentity current) {
            if (isOwned)
                return;

            if (current == null) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RideDiagnostics.LogReleased(this, _ship, ShipRideDiagnostics.CAUSE_RIDE_ENDED);
#endif
                ReleaseFromPlatform();
                return;
            }

            if (current.TryGetComponent(out ShipPoseSync poseSync) && poseSync.Ship != null)
                BindRemote(poseSync.Ship);
        }

        private float ResolveRideRestY() {
            if (_floating == null || _ship.DeckGeometry.TryGetDeckSurfaceY(_localOffset, out float deckSurfaceY) == false)
                return _localOffset.y;

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

        [Command]
        private void CmdSetRideOffset(Vector3 local, double time) {
            _syncedOffsetTime = time;
            _syncedLocalOffset = local;
        }

        [Command]
        private void CmdBeginRide(Vector3 local, double time, NetworkIdentity ship) {
            _syncedOffsetTime = time;
            _syncedLocalOffset = local;
            _syncedRideShip = ship;
        }

        [Command]
        private void CmdEndRide() {
            _syncedRideShip = null;
        }

        [ClientRpc]
        private void RpcRelease() {
            if (isServer)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RideDiagnostics.LogReleased(this, _ship, ShipRideDiagnostics.CAUSE_SERVER_RELEASE);
#endif
            ReleaseFromPlatform();
        }

        [Command]
        private void CmdSetSteer(float lateral) {
            if (_ship != null)
                _ship.Seats.ServerSetSteer(netId, lateral);
        }

        [Command]
        private void CmdStand() {
            if (_ship != null)
                _ship.Seats.ServerStand(this);
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
