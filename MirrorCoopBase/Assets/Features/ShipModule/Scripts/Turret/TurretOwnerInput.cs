using Features.CameraModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;

namespace Features.ShipModule.Scripts.Turret {
    // The seated owner's controls of a turret: look turns it, the fire button is the trigger, the interact button
    // stands up. A window with a free cursor takes the controls away.
    public sealed class TurretOwnerInput {
        private readonly IInputService _inputService;
        private readonly CursorModel _cursorModel;
        private readonly HoveredInteractableModel _hoveredInteractableModel;

        private Vector2 _pendingLook;
        private bool _isFirePressed;
        private bool _isStandRequested;
        private bool _isListening;

        public bool IsFireHeld => _isFirePressed && IsControlFree();

        public TurretOwnerInput(IInputService inputService, CursorModel cursorModel, HoveredInteractableModel hoveredInteractableModel) {
            _inputService = inputService;
            _cursorModel = cursorModel;
            _hoveredInteractableModel = hoveredInteractableModel;
        }

        public void Listen() {
            if (_isListening)
                return;

            _isListening = true;
            _pendingLook = Vector2.zero;
            _isFirePressed = false;
            _isStandRequested = false;
            _inputService.Look.VectorChangedPerformed += HandleLook;
            _inputService.Fire.Started += HandleFireStarted;
            _inputService.Fire.Canceled += HandleFireCanceled;
            _inputService.Grab.Performed += HandleInteract;
        }

        public void StopListening() {
            if (_isListening == false)
                return;

            _isListening = false;
            _isFirePressed = false;
            _inputService.Look.VectorChangedPerformed -= HandleLook;
            _inputService.Fire.Started -= HandleFireStarted;
            _inputService.Fire.Canceled -= HandleFireCanceled;
            _inputService.Grab.Performed -= HandleInteract;
        }

        public Vector2 ConsumeLook() {
            Vector2 look = IsControlFree() ? _pendingLook : Vector2.zero;
            _pendingLook = Vector2.zero;
            return look;
        }

        public bool ConsumeStandRequest() {
            bool isRequested = _isStandRequested;
            _isStandRequested = false;
            return isRequested;
        }

        private bool IsControlFree() =>
            _cursorModel.IsFreeCursorRequested == false && Cursor.lockState == CursorLockMode.Locked;

        private void HandleLook(Vector2 delta) =>
            _pendingLook += delta;

        private void HandleFireStarted() =>
            _isFirePressed = true;

        private void HandleFireCanceled() =>
            _isFirePressed = false;

        // A ready interactable under the aim gets the press from the player's interaction instead (it may be this
        // very seat, which stands the player up the same way).
        private void HandleInteract() {
            if (_hoveredInteractableModel.TryGetCurrent(out Component _, out bool isReady) && isReady)
                return;

            _isStandRequested = true;
        }
    }
}
