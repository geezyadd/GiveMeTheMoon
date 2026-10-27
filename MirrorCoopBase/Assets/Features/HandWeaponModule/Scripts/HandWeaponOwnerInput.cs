using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;

namespace Features.HandWeaponModule.Scripts {
    // The holder's fire button for a weapon in the hand. A window with a free cursor or a control block (death, a
    // window) takes it away, so a click on a window never fires.
    public sealed class HandWeaponOwnerInput {
        private readonly IInputService _inputService;
        private readonly CursorModel _cursorModel;
        private readonly PlayerControlBlockModel _playerControlBlockModel;

        private bool _isFirePressed;
        private bool _isListening;

        public bool IsFireHeld => _isFirePressed && IsControlFree();

        public HandWeaponOwnerInput(IInputService inputService, CursorModel cursorModel, PlayerControlBlockModel playerControlBlockModel) {
            _inputService = inputService;
            _cursorModel = cursorModel;
            _playerControlBlockModel = playerControlBlockModel;
        }

        public void Listen() {
            if (_isListening)
                return;

            _isListening = true;
            _isFirePressed = false;
            _inputService.Fire.Started += HandleFireStarted;
            _inputService.Fire.Canceled += HandleFireCanceled;
        }

        public void StopListening() {
            if (_isListening == false)
                return;

            _isListening = false;
            _isFirePressed = false;
            _inputService.Fire.Started -= HandleFireStarted;
            _inputService.Fire.Canceled -= HandleFireCanceled;
        }

        // The game's own cursor state, not the OS lock: a window without focus (a second test client) still plays.
        private bool IsControlFree() =>
            _playerControlBlockModel.IsBlocked == false && _cursorModel.IsFreeCursorRequested == false;

        private void HandleFireStarted() =>
            _isFirePressed = true;

        private void HandleFireCanceled() =>
            _isFirePressed = false;
    }
}
