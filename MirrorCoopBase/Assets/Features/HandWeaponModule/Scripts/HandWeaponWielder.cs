using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.GrabModule.Scripts;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;
using Zenject;

namespace Features.HandWeaponModule.Scripts {
    // A player's hand as the holder of the weapon in it, on every peer. While the hand holds a hand weapon item, the
    // player's weapon holder holds it (WeaponHolderSync.Equip): the local player's weapon system then fires it. On the
    // holder's own peer the shot goes from the muzzle to where the crosshair points; every other peer turns the weapon
    // along the synced aim. Everything ends when the item leaves the hand or is gone.
    public sealed class HandWeaponWielder : MonoBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private WeaponHolderSync _weaponSync;

        private IGameCameraService _gameCameraService;
        private IShotDeliveryContext _shotDeliveryContext;
        private HandWeaponPoseDriver _poseDriver;

        private Grabbable _resolvedGrabbable;
        private HandWeaponItem _resolvedItem;
        private HandWeaponItem _item;
        private HandWeaponHolder _holder;
        private CrosshairAim _crosshairAim;
        private bool _isOwner;

        [Inject]
        private void InjectDependencies(IGameCameraService gameCameraService, GameCameraModel gameCameraModel, IShotDeliveryContext shotDeliveryContext) {
            _gameCameraService = gameCameraService;
            _shotDeliveryContext = shotDeliveryContext;
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

            if (_item == null)
                return;

            if (_isOwner)
                AimAtCrosshair();
            else
                _holder.SetBodyAimDirection(_weaponSync.AimDirection);

            _poseDriver.Advance();
            _item.Feel.Advance(Time.deltaTime);
        }

        private void BeginHolding(HandWeaponItem item) {
            _item = item;
            _holder = new HandWeaponHolder(item.Feel.Muzzle, transform);
            _isOwner = _weaponSync.isOwned;
            if (_isOwner)
                _crosshairAim = new CrosshairAim(_shotDeliveryContext.CreateRaycast(item.HandDefinition.CrosshairLayers), _weaponSync.netId);

            _weaponSync.Equip(item, _holder);
            _poseDriver.Begin(item, _grab.ArmPoint, _holder, _isOwner);
        }

        private void EndHolding() {
            if (_holder == null)
                return;

            _poseDriver.End();
            if (_weaponSync != null)
                _weaponSync.Unequip();

            _isOwner = false;
            _item = null;
            _holder = null;
            _crosshairAim = null;
        }

        private void AimAtCrosshair() {
            _holder.AimAlong(_crosshairAim.GetShotDirection(_gameCameraService.OutputCamera.transform, _holder.Origin, _item.Definition.RangeMeters));
            _weaponSync.OwnerSetAim(_holder.BodyAimDirection);
        }

        private bool IsHolding(HandWeaponItem held) =>
            _item != null && ReferenceEquals(held, _item) && held != null;

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
