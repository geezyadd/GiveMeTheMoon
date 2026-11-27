using Features.CameraModule.Scripts.Services;
using Features.GrabModule.Scripts;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;
using Zenject;

namespace Features.HandWeaponModule.Scripts {
    // A player's hand as the holder of the weapon in it, on every peer. While the hand holds a hand weapon item, the
    // player's weapon holder holds it (WeaponHolderSync.Equip): the local player's weapon system then fires it. On the
    // holder's own peer the shot goes from the muzzle to where the crosshair points, and that aim is sent on as the
    // view turns (HandWeaponAimSync); every other peer turns the weapon smoothly along the synced aim. Everything ends
    // when the item leaves the hand or is gone.
    public sealed class HandWeaponWielder : MonoBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private WeaponHolderSync _weaponSync;
        [SerializeField] private HandWeaponGrip _grip;

        private IGameCameraService _gameCameraService;
        private IShotDeliveryContext _shotDeliveryContext;
        private HandWeaponPoseDriver _poseDriver;

        private Grabbable _resolvedGrabbable;
        private HandWeaponItem _resolvedItem;
        private HandWeaponItem _item;
        private HandWeaponHolder _holder;
        private CrosshairAim _crosshairAim;
        private HandWeaponAimSync _aimSync;
        private bool _isOwner;

        [Inject]
        private void InjectDependencies(IGameCameraService gameCameraService, IShotDeliveryContext shotDeliveryContext) {
            _gameCameraService = gameCameraService;
            _shotDeliveryContext = shotDeliveryContext;
            _poseDriver = new HandWeaponPoseDriver(gameCameraService, _grip);
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

            // A turret seat took the holder over and has let go: the weapon in the hand is held again.
            if (_weaponSync.Weapon == null)
                _weaponSync.Equip(_item, _holder);

            if (_isOwner)
                AimAtCrosshair();
            else
                _holder.SetBodyAimDirection(_aimSync.Smooth(_holder.BodyAimDirection, _weaponSync.AimDirection, Time.deltaTime));

            _poseDriver.Advance();
            _item.Feel.Advance(Time.deltaTime);
        }

        private void BeginHolding(HandWeaponItem item) {
            _item = item;
            _holder = new HandWeaponHolder(item.Feel.Muzzle, transform);
            _isOwner = _weaponSync.isOwned;
            HandWeaponDefinition definition = item.HandDefinition;
            _aimSync = new HandWeaponAimSync(definition.AimSendIntervalSeconds, definition.AimSendMinDegrees, definition.RemoteAimSmoothingSeconds);
            if (_isOwner == false)
                _holder.SetBodyAimDirection(_weaponSync.AimDirection);

            if (_isOwner)
                _crosshairAim = new CrosshairAim(_shotDeliveryContext.CreateRaycast(definition.CrosshairLayers), _weaponSync.netId, definition.MinAimDistance);

            _weaponSync.Equip(item, _holder);
            _poseDriver.Begin(item, _grab.ArmPoint, _holder, _isOwner);
        }

        private void EndHolding() {
            if (_holder == null)
                return;

            _poseDriver.End();
            if (_weaponSync != null)
                _weaponSync.Unequip(_item);

            _isOwner = false;
            _item = null;
            _holder = null;
            _crosshairAim = null;
            _aimSync = null;
        }

        private void AimAtCrosshair() {
            _holder.AimAlong(_crosshairAim.GetShotDirection(_gameCameraService.OutputCamera.transform, _grip.GripPoint, _item.Definition.RangeMeters));
            Vector3 aim = _holder.BodyAimDirection;
            _weaponSync.OwnerSetAim(aim);
            double now = Time.unscaledTimeAsDouble;
            if (_aimSync.IsSendDue(aim, now) == false)
                return;

            _weaponSync.OwnerSendAim(aim, _weaponSync.OwnerIsTriggerHeld);
            _aimSync.MarkSent(aim, now);
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
