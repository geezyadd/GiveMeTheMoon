using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.WeaponModule.Scripts.Anchors;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Readiness;
using Features.WeaponModule.Scripts.Shooting;
using UnityEngine;
using Zenject;

namespace Features.HandWeaponModule.Scripts {
    // A player's hand as the holder of the weapon in it, on every peer. While the hand holds a hand weapon item: on
    // the server the player holds that weapon, so its hit reports are accepted; on the owner's peer the fire button is
    // the trigger, the shot goes from the muzzle to where the crosshair points and the HUD shows the cooldown; every
    // other peer replays the shots from the synced trigger and aim. On every peer the weapon is an anchor of its
    // player (a rope starts there). A shot pushes its owner back while drifting overboard. Everything ends when the item
    // leaves the hand or is gone.
    public sealed class HandWeaponWielder : MonoBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private WeaponHolderSync _weaponSync;
        [SerializeField] private CharacterDrift _drift;

        private IWeaponShooterFactory _weaponShooterFactory;
        private IWeaponCatalogIndexService _weaponCatalogIndexService;
        private IGameCameraService _gameCameraService;
        private IShotDeliveryContext _shotDeliveryContext;
        private IWeaponHolderAnchorRegistry _weaponHolderAnchorRegistry;
        private HeldWeaponReadinessModel _heldWeaponReadinessModel;
        private HandWeaponOwnerInput _ownerInput;
        private HandWeaponPoseDriver _poseDriver;

        private Grabbable _resolvedGrabbable;
        private HandWeaponItem _resolvedItem;
        private HandWeaponItem _item;
        private HandWeaponHolder _holder;
        private HandWeaponTriggerLatch _triggerLatch;
        private CrosshairAim _crosshairAim;
        private WeaponShooter _shooter;
        private bool _isOwner;
        private bool _isServerHolding;
        private bool _isSentTriggerHeld;
        private bool _isAnchorRegistered;

        [Inject]
        private void InjectDependencies(
            IInputService inputService,
            CursorModel cursorModel,
            PlayerControlBlockModel playerControlBlockModel,
            IWeaponShooterFactory weaponShooterFactory,
            IWeaponCatalogIndexService weaponCatalogIndexService,
            IGameCameraService gameCameraService,
            GameCameraModel gameCameraModel,
            IShotDeliveryContext shotDeliveryContext,
            IWeaponHolderAnchorRegistry weaponHolderAnchorRegistry,
            HeldWeaponReadinessModel heldWeaponReadinessModel) {
            _weaponShooterFactory = weaponShooterFactory;
            _weaponCatalogIndexService = weaponCatalogIndexService;
            _gameCameraService = gameCameraService;
            _shotDeliveryContext = shotDeliveryContext;
            _weaponHolderAnchorRegistry = weaponHolderAnchorRegistry;
            _heldWeaponReadinessModel = heldWeaponReadinessModel;
            _ownerInput = new HandWeaponOwnerInput(inputService, cursorModel, playerControlBlockModel);
            _poseDriver = new HandWeaponPoseDriver(gameCameraService, gameCameraModel);
        }

        private void OnDestroy() =>
            EndHolding();

        private void Update() {
            HandWeaponItem held = FindHeldItem();
            if (IsHolding(held) == false) {
                EndHolding();
                if (held != null)
                    BeginHolding(held);
            }

            if (_shooter == null)
                return;

            float deltaTime = Time.deltaTime;
            if (_isOwner)
                AdvanceOwner(deltaTime);
            else
                AdvanceReplay();

            _poseDriver.Advance();
            int shots = _shooter.Advance(_holder, deltaTime);
            _item.Feel.Advance(deltaTime);
            if (_isOwner == false)
                return;

            _heldWeaponReadinessModel.SetReadiness(_shooter.Readiness);
            if (shots > 0)
                _drift.OwnerPush(-_holder.Aim.Direction * (_item.Definition.DriftRecoilMetersPerSecond * shots));
        }

        private void BeginHolding(HandWeaponItem item) {
            WeaponDefinition weapon = item.Definition.Weapon;
            int weaponIndex = _weaponCatalogIndexService.GetIndex(weapon);
            _item = item;
            _holder = new HandWeaponHolder(item.Feel.Muzzle, transform);
            _triggerLatch = new HandWeaponTriggerLatch(item.Definition.MinTriggerReportSeconds);
            if (_weaponSync.isServer) {
                _weaponSync.ServerBeginHolding(weapon, weaponIndex, _holder);
                _isServerHolding = true;
            }

            _isOwner = _weaponSync.isOwned;
            if (_isOwner)
                BeginOwnerControl(weaponIndex);

            IShotOutcomeReceiver outcomes = _isOwner
                ? new ReportedShotOutcomes(item.Feel, _weaponSync)
                : new ShownShotOutcomes(item.Feel);
            _shooter = _weaponShooterFactory.Create(weapon, item.Feel, outcomes);
            _poseDriver.Begin(item, _grab.ArmPoint, _holder, _isOwner);
            RegisterAnchor(weapon);
        }

        private void EndHolding() {
            UnregisterAnchor();
            _poseDriver.End();
            if (_isOwner)
                EndOwnerControl();

            if (_isServerHolding)
                _weaponSync.ServerEndHolding();

            _isServerHolding = false;
            _isOwner = false;
            _item = null;
            _holder = null;
            _shooter = null;
        }

        // A player whose key has not arrived yet holds no anchor: nothing can be hooked by it.
        private void RegisterAnchor(WeaponDefinition weapon) {
            if (_grab.Key.IsEmpty)
                return;

            _weaponHolderAnchorRegistry.Register(_grab.Key, weapon.Id, _holder);
            _isAnchorRegistered = true;
        }

        private void UnregisterAnchor() {
            if (_isAnchorRegistered)
                _weaponHolderAnchorRegistry.Unregister(_grab.Key);

            _isAnchorRegistered = false;
        }

        private void BeginOwnerControl(int weaponIndex) {
            _isSentTriggerHeld = false;
            _crosshairAim = new CrosshairAim(_shotDeliveryContext.CreateRaycast(_item.Definition.CrosshairLayers));
            _ownerInput.Listen();
            _weaponSync.OwnerBeginHolding(weaponIndex);
            _item.Feel.SetViewOwned(true);
        }

        // The item may already be gone (installed, destroyed with its scene) when the hand lets go.
        private void EndOwnerControl() {
            _ownerInput.StopListening();
            _heldWeaponReadinessModel.Clear();
            if (_item != null)
                _item.Feel.SetViewOwned(false);

            if (_isSentTriggerHeld)
                _weaponSync.OwnerSendAim(_holder.BodyAimDirection, false);

            _weaponSync.OwnerEndHolding();
        }

        private void AdvanceOwner(float deltaTime) {
            bool isTriggerHeld = _triggerLatch.Advance(_ownerInput.IsFireHeld, deltaTime);
            _holder.AimAlong(_crosshairAim.GetShotDirection(_gameCameraService.OutputCamera.transform, _holder.Origin, _item.Definition.Weapon.RangeMeters));
            _holder.SetTriggerHeld(isTriggerHeld);
            if (isTriggerHeld == _isSentTriggerHeld)
                return;

            _weaponSync.OwnerSendAim(_holder.BodyAimDirection, isTriggerHeld);
            _isSentTriggerHeld = isTriggerHeld;
        }

        private void AdvanceReplay() {
            _holder.SetBodyAimDirection(_weaponSync.AimDirection);
            _holder.SetTriggerHeld(_weaponSync.IsTriggerHeld);
        }

        private bool IsHolding(HandWeaponItem held) =>
            _shooter != null && ReferenceEquals(held, _item) && held != null;

        private HandWeaponItem FindHeldItem() {
            Grabbable held = _grab.Held;
            if (ReferenceEquals(held, _resolvedGrabbable))
                return _resolvedItem;

            _resolvedGrabbable = held;
            _resolvedItem = held != null && held.TryGetComponent(out HandWeaponItem item) ? item : null;
            return _resolvedItem;
        }
    }
}
