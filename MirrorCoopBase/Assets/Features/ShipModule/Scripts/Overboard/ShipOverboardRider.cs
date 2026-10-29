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

        private readonly WorldShiftSafeVelocity _shipVelocity = new WorldShiftSafeVelocity();
        private readonly ShipOverboardPullMotion _pullMotion = new ShipOverboardPullMotion();

        private float _maxRopeMeters;
        private ICameraShakeHandle _viewSway;

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

            ShipBase ship = _shipClientBindingModel.Ship;
            if (ship == null) {
                _shipVelocity.Reset();
                _drift.OwnerClearPull();
                return;
            }

            _shipVelocity.Track(ship.transform.position, Time.deltaTime);
            if (IsHooked)
                _drift.OwnerSetPullVelocity(_pullMotion.Step(transform.position, PullTarget(ship), _shipVelocity.Velocity, _pullSpeed, Time.fixedDeltaTime));
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

        // A ship without a deck stand point is broken data: a rope must not reel the player to its pivot.
        private Vector3 PullTarget(ShipBase ship) {
            if (ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 middle) == false)
                throw new System.InvalidOperationException(ship.name + " has no deck stand point.");

            return ship.transform.TransformPoint(middle + Vector3.up * _shipOverboardConfiguration.PullTargetHeightMeters);
        }

        private void ShowOverboard(bool isOverboard) {
            _hookVolume.enabled = isOverboard && isOwned == false;
            if (isOverboard) {
                _drift.Begin(_shipOverboardConfiguration.Drift, isOwned);
                BeginViewSway();
                return;
            }

            _drift.End();
            _shipVelocity.Reset();
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
