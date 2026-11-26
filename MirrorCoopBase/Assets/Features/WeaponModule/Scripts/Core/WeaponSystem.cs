using System;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;
using Features.WeaponModule.Scripts.Readiness;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Core {
    // The local player's controls of the weapon they hold, whatever it is: the fire button is the trigger, the reload
    // button asks the magazine to reload, and the HUD ring shows how ready the next shot is (the reload while one
    // runs). A window with a free cursor or a control block (death, a window) takes the trigger away.
    public sealed class WeaponSystem : IInitializable, ITickable, IDisposable {
        private readonly IWeaponService _weaponService;
        private readonly IInputService _inputService;
        private readonly CursorModel _cursorModel;
        private readonly PlayerControlBlockModel _playerControlBlockModel;
        private readonly HeldWeaponReadinessModel _readinessModel;

        private bool _isFirePressed;
        private bool _isReloadRequested;

        public WeaponSystem(
            IWeaponService weaponService,
            IInputService inputService,
            CursorModel cursorModel,
            PlayerControlBlockModel playerControlBlockModel,
            HeldWeaponReadinessModel readinessModel) {
            _weaponService = weaponService;
            _inputService = inputService;
            _cursorModel = cursorModel;
            _playerControlBlockModel = playerControlBlockModel;
            _readinessModel = readinessModel;
        }

        public void Initialize() {
            _inputService.Fire.Started += HandleFireStarted;
            _inputService.Fire.Canceled += HandleFireCanceled;
            _inputService.Reload.Started += HandleReloadStarted;
        }

        public void Dispose() {
            _inputService.Fire.Started -= HandleFireStarted;
            _inputService.Fire.Canceled -= HandleFireCanceled;
            _inputService.Reload.Started -= HandleReloadStarted;
        }

        public void Tick() {
            bool isReloadRequested = _isReloadRequested;
            _isReloadRequested = false;
            if (_weaponService.HasWeapon == false) {
                _readinessModel.Clear();
                return;
            }

            bool isControlFree = IsControlFree();
            if (isReloadRequested && isControlFree)
                _weaponService.Holder.OwnerRequestReload();

            _weaponService.Holder.OwnerAdvance(_isFirePressed && isControlFree, Time.deltaTime);
            _readinessModel.SetReadiness(_weaponService.Holder.OwnerReadiness);
        }

        // The game's own cursor state, not the OS lock: a window without focus (a second test client) still plays.
        private bool IsControlFree() =>
            _playerControlBlockModel.IsBlocked == false && _cursorModel.IsFreeCursorRequested == false;

        private void HandleFireStarted() =>
            _isFirePressed = true;

        private void HandleFireCanceled() =>
            _isFirePressed = false;

        private void HandleReloadStarted() =>
            _isReloadRequested = true;
    }
}
