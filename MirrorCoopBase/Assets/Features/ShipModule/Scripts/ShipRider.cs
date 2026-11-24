using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Debug;
#endif
using Features.ShipModule.Scripts.Riding;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    // A player's ride on a ship: the network side (the synced offset, ship, seat and key, the commands) and the order of
    // a frame. The work is in Riding/: the body bound to the deck (ShipRideBody), the owner's walk and vertical on the
    // deck (ShipDeckWalk), the standing offset over the network (ShipRideOffsetStream) and the seat (ShipSeatRide).
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

        // The player's stable key, written by the server at spawn: seats hold it, not the netId, which a rejoin changes.
        [SyncVar]
        private string _playerKey;

        private const float MAX_RIDE_SPEED = 120f;

        private IPlayerIdentityService _playerIdentityService;
        private ShipRiderKeyModel _shipRiderKeyModel;
        private DeckRideModeModel _deckRideModeModel;

        private ShipRideBody _body;
        private CharacterMovableBase _movable;
        private ShipDeckWalk _deckWalk;
        private readonly ShipRideOffsetStream _offsetStream = new ShipRideOffsetStream();
        private readonly ShipSeatRide _seatRide = new ShipSeatRide();

        private bool _bound;
        private Vector3 _localOffset;
        private ShipBase _ship;
        private Transform _platform;
        private Vector3 _rideVelocity;
        private float _reboardAt;

        internal Vector3 DebugLocalOffset => _localOffset;
        public bool IsRiding => _bound;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal ShipRideDiagnostics RideDiagnostics { get; } = new ShipRideDiagnostics();
#endif
        public bool IsSeated => _seated;
        private bool IsFollowingSeatPoint => _seatRide.IsFollowingSeatPoint(_seated);
        public PlayerKey Key => new PlayerKey(_playerKey);

        private void Awake() {
            EnsureParts();
            _movable = GetComponent<CharacterMovableBase>();
        }

        [Inject]
        private void InjectDependencies(
            CharacterInputBuffer characterInputBuffer,
            IGameCameraService gameCameraService,
            IPlayerIdentityService playerIdentityService,
            ShipRiderKeyModel shipRiderKeyModel,
            DeckRideModeModel deckRideModeModel) {
            EnsureParts();
            _deckWalk.SetInput(characterInputBuffer, gameCameraService);
            _playerIdentityService = playerIdentityService;
            _shipRiderKeyModel = shipRiderKeyModel;
            _deckRideModeModel = deckRideModeModel;
        }

        // This peer's own player stands on the deck as a physical body the ship carries (CarrierRider), not bound here;
        // only a seat binds it.
        private bool StandsAsCarried => isOwned && _deckRideModeModel != null && _deckRideModeModel.UsesCarrier;

        public override void OnStartServer() {
            _playerKey = _playerIdentityService.GetKey(connectionToClient).Id;
            _shipRiderKeyModel.Register(this);
        }

        // A client gets the key with the spawn, before this; on the host the server start registered it already.
        public override void OnStartClient() =>
            _shipRiderKeyModel.Register(this);

        public override void OnStopServer() {
            _shipRiderKeyModel.Unregister(this);
            if (_ship != null)
                _ship.ServerRemoveRider(this);
        }

        public override void OnStopClient() =>
            _shipRiderKeyModel.Unregister(this);

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

            if (_seated == false && StandsAsCarried)
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
            _deckWalk.Begin(ship.DeckGeometry, _localOffset, localVerticalSpeed, isOwned);
            _offsetStream.Reset(_syncedOffsetTime, _localOffset);
            _deckWalk.TrackPlatformRotation(_platform.rotation);
            _bound = true;
            _body.Attach(_platform, _localOffset);
            if (_movable != null)
                _movable.SetHeld(true);

            if (isOwned) {
                _syncedLocalOffset = _localOffset;
                CmdBeginRide(_localOffset, NetworkTime.time, ship.NetIdentity);
            }

            _body.SilenceNetworkBody();
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
            _offsetStream.Reset(NetworkTime.time, localOffset);
            if (_platform == null)
                return;

            _body.PlaceAt(_platform, _localOffset);
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
            _syncedRideShip = null;
            if (_bound)
                ReleaseFromPlatform();

            RpcRelease();
        }

        internal void ReleaseFromPlatform() =>
            Unbind(isOwned && _ship.IsFlying);

        // The ship goes away (its map unloads or it is destroyed): the rider is freed where it is, without the ride's
        // velocity; the server also clears the ride and the seat the other peers read.
        internal void ReleaseFromLeavingShip() {
            if (isServer) {
                _seated = false;
                _syncedRideShip = null;
            }

            Unbind(false);
        }

        private void Unbind(bool carryVelocity) {
            if (_bound == false)
                return;

            // The ship may have been snapped (origin shift at the station) since the last Follow.
            _body.PlaceAt(_platform, _localOffset);
            if (carryVelocity)
                _reboardAt = Time.time + _ship.ReboardDelaySeconds;
            _bound = false;
            _platform = null;
            if (isServer)
                _seated = false;

            _body.Detach(carryVelocity, _rideVelocity);
            if (_movable != null)
                _movable.SetHeld(false);
            _body.RestoreNetworkBody();
            if (isOwned && NetworkClient.ready)
                CmdEndRide();
        }

        internal void ServerLockSeat(Vector3 localOffset, NetworkIdentity ship) {
            _seated = true;
            _localOffset = localOffset;
            _syncedOffsetTime = NetworkTime.time;
            _syncedLocalOffset = localOffset;
            _syncedRideShip = ship;
        }

        // A seat station's own stand-up button (a turret's interact key) goes the same way as the jump.
        public void OwnerRequestStand() {
            if (isOwned && _seated)
                CmdStand();
        }

        // What a seat station needs from its seated owner on the server (the helm's steer); the station decides how often.
        internal void OwnerSendSeatInput(float value) {
            if (isOwned && _seated)
                CmdSetSeatInput(value);
        }

        internal void ServerUnlockSeat() {
            if (_seated && TryMoveToStandingSpot())
                ServerSyncStandingSpot();

            _seated = false;
        }

        // A seat station whose seat point moves with it (a turret's yaw) has the seated rider follow that point on
        // every peer, from the aim that peer shows; no offset is sent while seated.
        public void FollowSeatPoint(Transform seatPoint) =>
            _seatRide.FollowSeatPoint(seatPoint);

        public void StopFollowingSeatPoint(Transform seatPoint) =>
            _seatRide.StopFollowingSeatPoint(seatPoint);

        // A moving seat point (a turret's) leaves the rider away from the offset it sat down at: the others stand it up
        // where the server saw it.
        private void ServerSyncStandingSpot() {
            _syncedOffsetTime = NetworkTime.time;
            _syncedLocalOffset = _localOffset;
        }

        private bool TryMoveToStandingSpot() {
            if (_bound == false || _seatRide.TryResolveStandingSpot(_localOffset, _ship.DeckGeometry, _deckInset, out Vector3 standingOffset) == false)
                return false;

            _localOffset = standingOffset;
            return true;
        }

        internal void Follow(float dt) {
            if (_bound == false || _platform == null)
                return;

            if (isOwned)
                FollowOwnedInput(dt);

            Vector3 next = RideOffset();
            _localOffset = next;
            _seatRide.Track(IsFollowingSeatPoint);
            Vector3 previousWorld = transform.position;
            _body.PlaceAt(_platform, _localOffset);
            TrackRideVelocity(previousWorld, dt);
            _deckWalk.TrackPlatformRotation(_platform.rotation);
            if (IsOffDeckEdge())
                DropOffDeck();
        }

        // The owner's frame: a jump press stands a seated rider up; a standing one walks, falls and jumps on the deck
        // and sends its offset.
        private void FollowOwnedInput(float dt) {
            bool jumped = _deckWalk.ConsumeJumpPress();
            _deckWalk.ReadVerticalInput(dt);
            if (_seated) {
                // The press that stands the rider up must not jump right after.
                _deckWalk.DropQueuedJump();
                if (jumped)
                    CmdStand();
                return;
            }

            if (_deckWalk.Walk(ref _localOffset, _ship, dt))
                SendOffsetIfNeeded();
            _deckWalk.SimulateVertical(ref _localOffset, dt);
            SendOffsetIfNeeded();
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

        // The owner lets go of a standing ride the same way as walking off the deck: the server releases it on every
        // peer, a client releases at once and ends the ride on the server.
        private void ReleaseStandingRider() {
            if (isServer) {
                ServerRelease();
                return;
            }

            ReleaseFromPlatform();
        }

        private void SnapToSyncedOffset() {
            _localOffset = _syncedLocalOffset;
            _offsetStream.Reset(_syncedOffsetTime, _syncedLocalOffset);
        }

        private void ResetVertical(float velocity) =>
            _deckWalk.Reset(_localOffset.y, velocity);

        private void SendOffsetIfNeeded() {
            if (_offsetStream.ShouldSend(_syncedLocalOffset, _localOffset))
                CmdSetRideOffset(_localOffset, NetworkTime.time);
        }

        private Vector3 RideOffset() {
            if (IsFollowingSeatPoint)
                return _seatRide.SeatPointOffset(_platform);

            if (isOwned || _seated)
                return _localOffset;

            return _offsetStream.Sample(NetworkTime.time);
        }

        // Zenject may inject before or after Awake: whichever comes first makes the parts, the other keeps them.
        private void EnsureParts() {
            _body ??= new ShipRideBody(transform, _rb, _floating, _netBody);
            _deckWalk ??= new ShipDeckWalk(_floating, _stats, _walkSpeed, _sprintSpeed, _deckInset);
        }

        private void OnSyncedOffsetChanged(Vector3 previous, Vector3 current) {
            if (isOwned)
                return;

            // A seated rider's offset only changes when it switches seats (_seated stays true): it moves there at once.
            if (_seated) {
                SnapToSyncedOffset();
                return;
            }

            _offsetStream.Add(_syncedOffsetTime, current);
        }

        // Standing up must not blend back to the offset from before the seat.
        private void OnSeatedChanged(bool previous, bool current) {
            if (isOwned && current == false) {
                TryMoveToStandingSpot();
                // Standing up starts from rest on the seat, never with a velocity left over from before sitting.
                ResetVertical(0f);
                // A player who stands as carried gets off the seat's binding at the standing spot; the ship carries it.
                if (_bound && StandsAsCarried)
                    ReleaseStandingRider();
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
        private void CmdSetSeatInput(float value) {
            if (_ship != null)
                _ship.Seats.ServerReceiveSeatInput(this, value);
        }

        [Command]
        private void CmdStand() {
            if (_ship != null)
                _ship.Seats.ServerStand(this);
        }
    }
}
