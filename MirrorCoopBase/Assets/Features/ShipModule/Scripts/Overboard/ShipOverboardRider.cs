using Features.CameraModule.Contracts;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // A player overboard, on every peer. The server decides when it goes overboard, when the ship takes it back, when
    // a rope hooks it and when the void takes it (ShipOverboardSystem); the synced state tells every peer. The owner
    // drifts its own body (the network body carries it to the others), sways its view, follows a rope that hooked it
    // and lands back on the deck through the ship's ride path; every peer shows the tumble and the volume the other
    // players' ropes hook. The drift is not restored on a rejoin: a player who leaves overboard comes back on the deck.
    public sealed class ShipOverboardRider : NetworkBehaviour {
        private const float MAX_SHIP_SPEED = 120f;
        private const float FULL_SWAY = 1f;

        [SerializeField] private ShipRider _rider;
        [SerializeField] private CharacterDrift _drift;
        [Tooltip("The trigger the other players' ropes hook while this player drifts; never on its own peer.")]
        [SerializeField] private Collider _hookVolume;

        [SyncVar]
        private double _overboardSince;

        [SyncVar(hook = nameof(OnOverboardChanged))]
        private bool _isOverboard;

        [SyncVar]
        private string _hookerKey = string.Empty;

        [SyncVar]
        private string _hookWeaponId = string.Empty;

        [SyncVar]
        private float _pullSpeed;

        private ShipOverboardConfiguration _shipOverboardConfiguration;
        private IComponentRegistry<ShipOverboardRider> _registry;
        private IShipClientBindingModel _shipClientBindingModel;
        private ICameraShakeService _cameraShakeService;
        private IShipOverboardWeaponIds _shipOverboardWeaponIds;

        private float _maxRopeMeters;
        private ICameraShakeHandle _viewSway;
        private Vector3 _lastShipPosition;
        private Vector3 _shipVelocity;
        private bool _hasShipPosition;

        public bool IsOverboard => _isOverboard;
        public bool IsHooked => string.IsNullOrEmpty(_hookerKey) == false;
        public PlayerKey HookerKey => new PlayerKey(_hookerKey);
        public string HookWeaponId => _hookWeaponId;
        public PlayerKey Key => _rider.Key;
        public ulong WeaponTargetId => _shipOverboardWeaponIds.GetTargetId(netId);
        public Vector3 Position => transform.position;
        internal ShipRider Rider => _rider;
        // Server: rode the ship in this flight, so leaving its volume is going overboard.
        internal bool HasRiddenThisFlight { get; set; }
        internal float MaxRopeMeters => _maxRopeMeters;
        internal float DriftSeconds => (float)(NetworkTime.time - _overboardSince);

        [Inject]
        private void InjectDependencies(
            ShipOverboardConfiguration shipOverboardConfiguration,
            IComponentRegistry<ShipOverboardRider> registry,
            IShipClientBindingModel shipClientBindingModel,
            ICameraShakeService cameraShakeService,
            IShipOverboardWeaponIds shipOverboardWeaponIds) {
            _shipOverboardConfiguration = shipOverboardConfiguration;
            _registry = registry;
            _shipClientBindingModel = shipClientBindingModel;
            _cameraShakeService = cameraShakeService;
            _shipOverboardWeaponIds = shipOverboardWeaponIds;
        }

        public override void OnStartServer() =>
            _registry.Add(this);

        public override void OnStopServer() =>
            _registry.Remove(this);

        public override void OnStartClient() {
            _registry.Add(this);
            ShowOverboard(_isOverboard);
        }

        public override void OnStopClient() {
            _registry.Remove(this);
            ShowOverboard(false);
        }

        private void Update() {
            if (isOwned == false || _isOverboard == false)
                return;

            TrackShipVelocity(Time.deltaTime);
            if (IsHooked)
                _drift.OwnerSetPullVelocity(PullVelocity());
            else
                _drift.OwnerClearPull();
        }

        [Server]
        internal void ServerBeginOverboard() {
            _overboardSince = NetworkTime.time;
            _isOverboard = true;
            ShowOverboard(true);
        }

        // The ship took the player back: in flight its owner lands on the deck through the ride path.
        [Server]
        internal void ServerCapture(ShipBase ship) {
            ServerEnd();
            if (ship.IsFlying)
                TargetLandOnDeck(ship.NetIdentity);
        }

        [Server]
        internal void ServerEnd() {
            ServerUnhook();
            _isOverboard = false;
            ShowOverboard(false);
        }

        // Death, a leave or the ship going away: no drift, and the next one needs a new ride first.
        [Server]
        internal void ServerReset() {
            HasRiddenThisFlight = false;
            if (_isOverboard)
                ServerEnd();
        }

        [Server]
        internal void ServerHook(PlayerKey hooker, string weaponId, float pullSpeed, float maxRopeMeters) {
            _maxRopeMeters = maxRopeMeters;
            _hookerKey = hooker.Id;
            _hookWeaponId = weaponId;
            _pullSpeed = Mathf.Min(pullSpeed, _shipOverboardConfiguration.MaxPullSpeedMetersPerSecond);
        }

        [Server]
        internal void ServerUnhook() {
            _hookerKey = string.Empty;
            _hookWeaponId = string.Empty;
            _pullSpeed = 0f;
        }

        // The ship this peer shows goes away: nothing drifts on toward the next map.
        internal void EndOnShipLeave() {
            if (isServer)
                ServerReset();
            else
                ShowOverboard(false);
        }

        private Vector3 PullVelocity() {
            ShipBase ship = _shipClientBindingModel.Ship;
            if (ship == null)
                return Vector3.zero;

            Vector3 toTarget = PullTarget(ship) - transform.position;
            float step = Mathf.Min(_pullSpeed, toTarget.magnitude / Time.fixedDeltaTime);
            return _shipVelocity + toTarget.normalized * step;
        }

        private Vector3 PullTarget(ShipBase ship) {
            Vector3 middle = ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 standPoint) ? standPoint : Vector3.zero;
            return ship.transform.TransformPoint(middle + Vector3.up * _shipOverboardConfiguration.PullTargetHeightMeters);
        }

        private void TrackShipVelocity(float deltaTime) {
            ShipBase ship = _shipClientBindingModel.Ship;
            if (ship == null || deltaTime <= 0f) {
                _hasShipPosition = false;
                return;
            }

            Vector3 position = ship.transform.position;
            if (_hasShipPosition)
                _shipVelocity = ShipVelocityOrLast((position - _lastShipPosition) / deltaTime);

            _lastShipPosition = position;
            _hasShipPosition = true;
        }

        // A world shift moves the ship hundreds of metres in one frame; that is no velocity.
        private Vector3 ShipVelocityOrLast(Vector3 velocity) =>
            velocity.sqrMagnitude <= MAX_SHIP_SPEED * MAX_SHIP_SPEED ? velocity : _shipVelocity;

        private void ShowOverboard(bool isOverboard) {
            _hookVolume.enabled = isOverboard && isOwned == false;
            if (isOverboard) {
                _drift.Begin(_shipOverboardConfiguration.Drift, isOwned);
                BeginViewSway();
                return;
            }

            _drift.End();
            _hasShipPosition = false;
            EndViewSway();
        }

        private void BeginViewSway() {
            if (isOwned == false || _viewSway != null)
                return;

            _viewSway = _cameraShakeService.BeginShake(_shipOverboardConfiguration.DriftViewSway);
            _viewSway.SetIntensity(FULL_SWAY);
        }

        private void EndViewSway() {
            if (_viewSway == null)
                return;

            _viewSway.Stop();
            _viewSway = null;
        }

        [TargetRpc]
        private void TargetLandOnDeck(NetworkIdentity shipIdentity) {
            ShowOverboard(false);
            if (shipIdentity == null || shipIdentity.TryGetComponent(out ShipPoseSync poseSync) == false || poseSync.Ship == null)
                return;

            ShipBase ship = poseSync.Ship;
            ship.Riders.TrackRider(_rider);
            _rider.BindToPlatform(ship);
        }

        private void OnOverboardChanged(bool previous, bool current) =>
            ShowOverboard(current);
    }
}
