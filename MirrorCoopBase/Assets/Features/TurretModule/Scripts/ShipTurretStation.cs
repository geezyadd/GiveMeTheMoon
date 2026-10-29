using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Shooting;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.TurretModule.Scripts {
    // A turret's slot view as a seat station. On the seated owner's peer the mouse turns it at once, the fire button
    // fires and the view rides the camera point behind the gun; on every other peer the turret follows the synced aim,
    // smoothed, and replays the fire from the synced trigger. On the server the seated player holds the weapon, so
    // its hit reports are accepted. A broken turret module does not fire until it is repaired. The seat point turns with
    // the gun, and the seated player's body follows it on every peer.
    public sealed class ShipTurretStation : MonoBehaviour, ISeatStation {
        [SerializeField] private ShipTurretDefinition _definition;
        [SerializeField] private WeaponDefinition _weapon;
        [SerializeField] private Transform _yawPivot;
        [SerializeField] private Transform _pitchPivot;
        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private GameObject _hands;
        [SerializeField] private ShipSeat _seat;

        private readonly SeatedOwnerBody _ownerBody = new SeatedOwnerBody();

        private IGameCameraService _gameCameraService;
        private IWeaponShooterFactory _weaponShooterFactory;
        private IWeaponCatalogIndexService _weaponCatalogIndexService;
        private IShipModuleCondition _shipModuleCondition;
        private TurretOwnerInput _ownerInput;

        private ShipBase _ship;
        private ShipSocket _socket;
        private IWeaponFeelPresenter _feel;
        private TurretAim _aim;
        private TurretAim _receivedAim;
        private TurretAimSmoothing _remoteAim;
        private TurretAimSendSchedule _aimSendSchedule;
        private ShipTurretWeaponHolder _holder;
        private WeaponShooter _replayShooter;
        private WeaponShooter _ownerShooter;
        private Quaternion _yawRest;
        private Quaternion _pitchRest;
        private Transform _seatPoint;
        private int _weaponIndex;
        private PlayerKey _occupant;
        private WeaponHolderSync _occupantWeapon;
        private ShipRider _occupantRider;
        private bool _isOwner;
        private bool _isServerHolding;
        private bool _isWorking = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _isDebugTriggerHeld;
#endif

        public bool IsOccupiedByLocalPlayer => _isOwner;

        [Inject]
        private void InjectDependencies(
            IInputService inputService,
            CursorModel cursorModel,
            HoveredInteractableModel hoveredInteractableModel,
            PlayerControlBlockModel playerControlBlockModel,
            IGameCameraService gameCameraService,
            IWeaponShooterFactory weaponShooterFactory,
            IWeaponCatalogIndexService weaponCatalogIndexService,
            IShipModuleCondition shipModuleCondition) {
            _gameCameraService = gameCameraService;
            _weaponShooterFactory = weaponShooterFactory;
            _weaponCatalogIndexService = weaponCatalogIndexService;
            _shipModuleCondition = shipModuleCondition;
            _ownerInput = new TurretOwnerInput(inputService, cursorModel, hoveredInteractableModel, playerControlBlockModel);
        }

        private void Awake() {
            _ship = GetComponentInParent<ShipBase>();
            _socket = GetComponentInParent<ShipSocket>();
            _feel = GetComponent<IWeaponFeelPresenter>();
            _aim = new TurretAim(_definition);
            _receivedAim = new TurretAim(_definition);
            _remoteAim = new TurretAimSmoothing(_definition.RemoteAimSmoothingSeconds);
            _aimSendSchedule = new TurretAimSendSchedule(_definition.AimSendIntervalSeconds);
            _holder = new ShipTurretWeaponHolder(_feel.Muzzle);
            _yawRest = _yawPivot.localRotation;
            _pitchRest = _pitchPivot.localRotation;
            _seatPoint = _seat.SitPoint;
            _weaponIndex = _weaponCatalogIndexService.GetIndex(_weapon);
            _feel.Bind(_weapon.FeelProfile, new ShipDeckFeelFloor(_ship.transform, _ship.DeckGeometry));
            _replayShooter = _weaponShooterFactory.Create(_weapon, _feel, new ShownShotOutcomes(_feel));
            _hands.SetActive(false);
            _shipModuleCondition.OnConditionChanged += OnConditionChanged;
            OnConditionChanged();
        }

        private void OnDestroy() {
            _shipModuleCondition.OnConditionChanged -= OnConditionChanged;
            Exit();
        }

        private void Update() {
            float deltaTime = Time.deltaTime;
            if (_occupant.IsEmpty == false && _occupantWeapon == null)
                TryTakeOccupant();

            if (_isOwner)
                AdvanceOwner();
            else
                AdvanceReplay(deltaTime);

            ApplyPose();
            CurrentShooter().Advance(_holder, deltaTime);
            _feel.Advance(deltaTime);
        }

        public void Enter(PlayerKey occupant) {
            Exit();
            _occupant = occupant;
            _hands.SetActive(true);
            TryTakeOccupant();
        }

        public void Exit() {
            if (_isOwner)
                EndOwnerControl();

            if (_isServerHolding && _occupantWeapon != null)
                _occupantWeapon.ServerEndHolding();

            // The rider may already be gone with its player object.
            if (_occupantRider != null)
                _occupantRider.StopFollowingSeatPoint(_seatPoint);

            _isServerHolding = false;
            _occupant = default;
            _occupantWeapon = null;
            _occupantRider = null;
            _holder.SetTriggerHeld(false);
            _remoteAim.Reset();
            // OnDestroy reaches here too, when the hands may already be gone with the view.
            if (_hands != null)
                _hands.SetActive(false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test hooks for the loop autotest: aim the seated owner's turret at a point and hold its trigger.
        public void DebugAimAt(Vector3 worldPoint) =>
            _aim.SetDirection(Quaternion.Inverse(_yawPivot.parent.rotation) * (worldPoint - _pitchPivot.position));

        public void DebugHoldTrigger(bool isHeld) =>
            _isDebugTriggerHeld = isHeld;
#endif

        // The occupant's player object may spawn on this peer after the seat state (a late join): taken when it is there.
        private void TryTakeOccupant() {
            if (_ship.Riders.TryFindByKey(_occupant, out ShipRider rider) == false || rider.TryGetComponent(out WeaponHolderSync weapon) == false)
                return;

            _occupantWeapon = weapon;
            _occupantRider = rider;
            _occupantRider.FollowSeatPoint(_seatPoint);

            if (NetworkServer.active) {
                weapon.ServerBeginHolding(_weapon, _weaponIndex, _holder);
                _isServerHolding = true;
            }

            if (rider.isOwned)
                BeginOwnerControl();
        }

        private void BeginOwnerControl() {
            _isOwner = true;
            _aim.Set(_remoteAim.Yaw, _remoteAim.Pitch);
            _aimSendSchedule.Reset();
            _ownerInput.Listen();
            _gameCameraService.BeginFixedView(_cameraPoint);
            _ownerBody.Hide(_occupantWeapon);
            _occupantWeapon.OwnerBeginHolding(_weaponIndex);
            _ownerShooter = _weaponShooterFactory.Create(_weapon, _feel, new ReportedShotOutcomes(_feel, _occupantWeapon));
            _feel.SetViewOwned(true);
        }

        private void EndOwnerControl() {
            _isOwner = false;
            _ownerInput.StopListening();
            _gameCameraService.EndFixedView();
            _ownerBody.Show();
            _feel.SetViewOwned(false);
            _ownerShooter = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _isDebugTriggerHeld = false;
#endif
            if (_occupantWeapon == null)
                return;

            _occupantWeapon.OwnerSendAim(_aim.Direction, false);
            _occupantWeapon.OwnerEndHolding();
        }

        private void AdvanceOwner() {
            _aim.Turn(_ownerInput.ConsumeLook());
            bool isTriggerHeld = IsOwnerTriggerHeld() && _isWorking;
            _holder.SetTriggerHeld(isTriggerHeld);
            _remoteAim.SnapTo(_aim.Yaw, _aim.Pitch);
            SendAimIfDue(isTriggerHeld);
            if (_ownerInput.ConsumeStandRequest() && _occupantRider != null)
                _occupantRider.OwnerRequestStand();
        }

        private bool IsOwnerTriggerHeld() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_isDebugTriggerHeld)
                return true;
#endif
            return _ownerInput.IsFireHeld;
        }

        private void SendAimIfDue(bool isTriggerHeld) {
            double now = Time.unscaledTimeAsDouble;
            if (_aimSendSchedule.IsDue(isTriggerHeld, now) == false)
                return;

            _occupantWeapon.OwnerSendAim(_aim.Direction, isTriggerHeld);
            _aimSendSchedule.MarkSent(isTriggerHeld, now);
        }

        private void AdvanceReplay(float deltaTime) {
            bool hasOccupant = _occupantWeapon != null;
            _holder.SetTriggerHeld(hasOccupant && _occupantWeapon.IsTriggerHeld && _isWorking);
            if (hasOccupant)
                _receivedAim.SetDirection(_occupantWeapon.AimDirection);

            _remoteAim.Advance(_receivedAim.Yaw, _receivedAim.Pitch, deltaTime);
        }

        private void ApplyPose() {
            float yaw = _isOwner ? _aim.Yaw : _remoteAim.Yaw;
            float pitch = _isOwner ? _aim.Pitch : _remoteAim.Pitch;
            _yawPivot.localRotation = _yawRest * Quaternion.Euler(0f, yaw, 0f);
            _pitchPivot.localRotation = _pitchRest * Quaternion.Euler(-pitch, 0f, 0f);
        }

        private WeaponShooter CurrentShooter() =>
            _ownerShooter ?? _replayShooter;

        private void OnConditionChanged() =>
            _isWorking = _shipModuleCondition.IsWorking(_socket.SocketId);
    }
}
