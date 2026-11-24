using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.NetworkModelModule.Scripts;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Debug;
#endif
using Features.ShipModule.Scripts.Riding;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    // A player's seat on a ship: the network side (the synced seat offset, ship, seat and key, the commands) and the
    // order of a frame. A seated player is bound to the ship (ShipRideBody: kinematic, no collisions) at its seat offset
    // (ShipSeatRide: a moving seat point, the standing spot when getting up); standing up releases the binding. A
    // standing player is never bound: it stays a physical body the ship carries (ShipCarrier, CarrierRider).
    public sealed class ShipRider : NetworkBehaviour {
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private FloatingController _floating;
        [SerializeField] private float _deckInset = 0.08f;

        // The seat offset, written by the server when the rider sits down or stands up at a spot next to a moving seat.
        [SyncVar(hook = nameof(OnSyncedOffsetChanged))]
        private Vector3 _syncedLocalOffset;

        // The ship this rider is bound to (its pose-sync identity), null when not bound. Every peer that does not own
        // the rider binds it from this, at the seat offset.
        [SyncVar(hook = nameof(OnRideShipChanged))]
        private NetworkIdentity _syncedRideShip;

        [SyncVar(hook = nameof(OnSeatedChanged))]
        private bool _seated;

        // The player's stable key, written by the server at spawn: seats hold it, not the netId, which a rejoin changes.
        [SyncVar]
        private string _playerKey;

        private const float MAX_RIDE_SPEED = 120f;

        private CharacterInputBuffer _input;
        private IPlayerIdentityService _playerIdentityService;
        private ShipRiderKeyModel _shipRiderKeyModel;

        private ShipRideBody _body;
        private readonly ShipSeatRide _seatRide = new ShipSeatRide();

        private bool _bound;
        private bool _jumpHeldBefore;
        private Vector3 _localOffset;
        private ShipBase _ship;
        private Transform _platform;
        private Vector3 _rideVelocity;

        internal Vector3 DebugLocalOffset => _localOffset;

        // Bound to a ship: seated, or for a moment after standing up until the release reaches this peer. A player
        // standing on the deck is not bound; IShipRiders.IsAboard tells whether it is on the ship.
        public bool IsRiding => _bound;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal ShipRideDiagnostics RideDiagnostics { get; } = new ShipRideDiagnostics();
#endif
        public bool IsSeated => _seated;
        private bool IsFollowingSeatPoint => _seatRide.IsFollowingSeatPoint(_seated);
        public PlayerKey Key => new PlayerKey(_playerKey);

        private void Awake() =>
            EnsureBody();

        [Inject]
        private void InjectDependencies(
            CharacterInputBuffer characterInputBuffer,
            IPlayerIdentityService playerIdentityService,
            ShipRiderKeyModel shipRiderKeyModel) {
            EnsureBody();
            _input = characterInputBuffer;
            _playerIdentityService = playerIdentityService;
            _shipRiderKeyModel = shipRiderKeyModel;
        }

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

        internal void BindToSeat(ShipBase ship, Vector3 seatOffset) {
            if (_bound) {
                _localOffset = seatOffset;
                return;
            }

            if (_rb != null && ship != null) {
                Bind(ship, seatOffset);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RideDiagnostics.LogBound(this, ship, ShipRideDiagnostics.CAUSE_SEAT);
#endif
            }
        }

        private void Bind(ShipBase ship, Vector3 localOffset) {
            _ship = ship;
            _platform = ship.transform;
            _localOffset = localOffset;
            // The jump that sat the rider down (or is still held) must not stand it up at once.
            _jumpHeldBefore = _input != null && _input.JumpHeld;
            _bound = true;
            _body.Attach(_platform, _localOffset);
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

            _localOffset = _syncedLocalOffset;
        }

        // Server-authoritative release (landing, wreck, stand up at a station): every peer releases this rider, the owner
        // with its ride velocity.
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

        // With carryVelocity the body leaves with the seat's velocity in the world, so the ship that carries it on
        // takes off its own speed and the player stands still on the deck.
        private void Unbind(bool carryVelocity) {
            if (_bound == false)
                return;

            // The ship may have been snapped (origin shift at the station) since the last Follow.
            _body.PlaceAt(_platform, _localOffset);
            _bound = false;
            _platform = null;
            if (isServer)
                _seated = false;

            _body.Detach(carryVelocity, _rideVelocity);
            if (isOwned && NetworkClient.ready)
                CmdEndRide();
        }

        internal void ServerLockSeat(Vector3 localOffset, NetworkIdentity ship) {
            _seated = true;
            _localOffset = localOffset;
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
                _syncedLocalOffset = _localOffset;

            _seated = false;
        }

        // A seat station whose seat point moves with it (a turret's yaw) has the seated rider follow that point on
        // every peer, from the aim that peer shows; no offset is sent while seated.
        public void FollowSeatPoint(Transform seatPoint) =>
            _seatRide.FollowSeatPoint(seatPoint);

        public void StopFollowingSeatPoint(Transform seatPoint) =>
            _seatRide.StopFollowingSeatPoint(seatPoint);

        // A moving seat point (a turret's) leaves the rider away from the offset it sat down at: it stands up at a free
        // spot next to it.
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
                StandUpOnJumpPress();

            if (IsFollowingSeatPoint)
                _localOffset = _seatRide.SeatPointOffset(_platform);

            _seatRide.Track(IsFollowingSeatPoint);
            Vector3 previousWorld = transform.position;
            _body.PlaceAt(_platform, _localOffset);
            TrackRideVelocity(previousWorld, dt);
        }

        // The owner's frame: a jump press stands a seated rider up.
        private void StandUpOnJumpPress() {
            bool jumpHeld = _input != null && _input.JumpHeld;
            bool pressed = jumpHeld && _jumpHeldBefore == false;
            _jumpHeldBefore = jumpHeld;
            if (pressed && _seated)
                CmdStand();
        }

        private void TrackRideVelocity(Vector3 previousWorld, float dt) {
            if (dt <= 0f)
                return;

            Vector3 velocity = (transform.position - previousWorld) / dt;
            // An origin shift moves the ship hundreds of metres in one frame; that is not a ride velocity.
            if (velocity.sqrMagnitude <= MAX_RIDE_SPEED * MAX_RIDE_SPEED)
                _rideVelocity = velocity;
        }

        // The owner lets go of the seat's binding after standing up in flight: the server releases it on every peer, a
        // client releases at once and ends the ride on the server.
        private void ReleaseStandingRider() {
            if (isServer) {
                ServerRelease();
                return;
            }

            ReleaseFromPlatform();
        }

        // Zenject may inject before or after Awake: whichever comes first makes the body, the other keeps it.
        private void EnsureBody() =>
            _body ??= new ShipRideBody(transform, _rb, _floating);

        // A seated rider's offset only changes when it switches seats or stands up next to a moving seat: it moves
        // there at once.
        private void OnSyncedOffsetChanged(Vector3 previous, Vector3 current) {
            if (isOwned == false)
                _localOffset = current;
        }

        // The owner stands up at the standing spot and gets off the seat's binding there; the ship carries it on. The
        // others take the server's offset, so they release it where the server stood it up.
        private void OnSeatedChanged(bool previous, bool current) {
            if (isOwned && current == false) {
                TryMoveToStandingSpot();
                if (_bound)
                    ReleaseStandingRider();
                return;
            }

            _localOffset = _syncedLocalOffset;
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
