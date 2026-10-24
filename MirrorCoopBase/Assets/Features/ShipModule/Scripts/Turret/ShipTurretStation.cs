using System;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts.Damage;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Shooting;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Turret {
    // A turret's slot view as a seat station. On the seated owner's peer the mouse turns it at once, the fire button
    // fires and the view rides the camera point behind the gun; on every other peer the turret follows the synced aim,
    // smoothed, and replays the fire from the synced trigger. On the server the seated player holds the weapon, so
    // its hit reports are accepted. A broken turret module does not fire until it is repaired.
    public sealed class ShipTurretStation : MonoBehaviour, ISeatStation {
        private const string WEAPON_NOT_IN_CATALOG_FORMAT = "{0}: weapon {1} is not in the {2}; add it there, the server checks every hit against the catalog.";

        [SerializeField] private ShipTurretDefinition _definition;
        [SerializeField] private WeaponDefinition _weapon;
        [SerializeField] private Transform _yawPivot;
        [SerializeField] private Transform _pitchPivot;
        [SerializeField] private Transform _cameraPoint;
        [SerializeField] private GameObject _hands;

        private IGameCameraService _gameCameraService;
        private IWeaponShooterFactory _weaponShooterFactory;
        private WeaponCatalog _weaponCatalog;
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
        private int _weaponIndex;
        private uint _occupantNetId;
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
            IGameCameraService gameCameraService,
            IWeaponShooterFactory weaponShooterFactory,
            WeaponCatalog weaponCatalog,
            IShipModuleCondition shipModuleCondition) {
            _gameCameraService = gameCameraService;
            _weaponShooterFactory = weaponShooterFactory;
            _weaponCatalog = weaponCatalog;
            _shipModuleCondition = shipModuleCondition;
            _ownerInput = new TurretOwnerInput(inputService, cursorModel, hoveredInteractableModel);
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
            _weaponIndex = FindWeaponIndex();
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
            if (_occupantNetId != 0 && _occupantWeapon == null)
                TryTakeOccupant();

            if (_isOwner)
                AdvanceOwner();
            else
                AdvanceReplay(deltaTime);

            ApplyPose();
            CurrentShooter().Advance(_holder, deltaTime);
            _feel.Advance(deltaTime);
        }

        public void Enter(uint occupantNetId) {
            Exit();
            _occupantNetId = occupantNetId;
            _hands.SetActive(true);
            TryTakeOccupant();
        }

        public void Exit() {
            if (_isOwner)
                EndOwnerControl();

            if (_isServerHolding && _occupantWeapon != null)
                _occupantWeapon.ServerEndHolding();

            _isServerHolding = false;
            _occupantNetId = 0;
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
            if (TryFindOccupant(out NetworkIdentity occupant) == false || occupant.TryGetComponent(out WeaponHolderSync weapon) == false)
                return;

            _occupantWeapon = weapon;
            occupant.TryGetComponent(out _occupantRider);
            if (NetworkServer.active) {
                weapon.ServerBeginHolding(_weapon, _weaponIndex, _holder);
                _isServerHolding = true;
            }

            if (occupant.isOwned)
                BeginOwnerControl();
        }

        private bool TryFindOccupant(out NetworkIdentity occupant) {
            occupant = null;
            if (NetworkClient.active && NetworkClient.spawned.TryGetValue(_occupantNetId, out occupant))
                return occupant != null;

            return NetworkServer.active && NetworkServer.spawned.TryGetValue(_occupantNetId, out occupant) && occupant != null;
        }

        private void BeginOwnerControl() {
            _isOwner = true;
            _aim.Set(_remoteAim.Yaw, _remoteAim.Pitch);
            _aimSendSchedule.Reset();
            _ownerInput.Listen();
            _gameCameraService.BeginFixedView(_cameraPoint);
            _occupantWeapon.OwnerBeginHolding(_weaponIndex);
            _ownerShooter = _weaponShooterFactory.Create(_weapon, _feel, new ReportedShotOutcomes(_feel, _occupantWeapon));
            _feel.SetViewOwned(true);
        }

        private void EndOwnerControl() {
            _isOwner = false;
            _ownerInput.StopListening();
            _gameCameraService.EndFixedView();
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

        private int FindWeaponIndex() {
            for (int i = 0; i < _weaponCatalog.Weapons.Count; i++) {
                if (_weaponCatalog.Weapons[i] == _weapon)
                    return i;
            }

            throw new InvalidOperationException(string.Format(WEAPON_NOT_IN_CATALOG_FORMAT, name, _weapon.Id, nameof(WeaponCatalog)));
        }

        private void OnConditionChanged() =>
            _isWorking = _shipModuleCondition.IsWorking(_socket.SocketId);
    }
}
