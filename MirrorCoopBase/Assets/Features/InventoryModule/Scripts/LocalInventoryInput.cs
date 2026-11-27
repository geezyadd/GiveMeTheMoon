using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.InventoryModule.Scripts {
    // The local player's inventory buttons: 1-4 pick a slot (the selected one again stows or takes out its item), the
    // wheel cycles the slots, the release button drops the item in the hand on a tap and throws it along the view
    // when held. Ignored while the controls are blocked (a window, death), seated or knocked down.
    public sealed class LocalInventoryInput : NetworkBehaviour {
        private const int FIRST_SLOT = 0;
        private const int SECOND_SLOT = 1;
        private const int THIRD_SLOT = 2;
        private const int FOURTH_SLOT = 3;

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private GrabController _grab;
        [SerializeField] private LocalPlayerInteraction _interaction;
        [SerializeField] private CharacterMovableBase _movable;

        private IInputService _inputService;
        private PlayerControlBlockModel _playerControlBlockModel;
        private IGameCameraService _gameCameraService;
        private ReleasePress _releasePress;

        [Inject]
        private void InjectDependencies(
            IInputService inputService,
            PlayerControlBlockModel playerControlBlockModel,
            IGameCameraService gameCameraService,
            InventoryConfiguration inventoryConfiguration) {
            _inputService = inputService;
            _playerControlBlockModel = playerControlBlockModel;
            _gameCameraService = gameCameraService;
            _releasePress = new ReleasePress(inventoryConfiguration.ThrowHoldSeconds);
        }

        public override void OnStartLocalPlayer() {
            _inputService.SelectSlot1.Performed += SelectFirstSlot;
            _inputService.SelectSlot2.Performed += SelectSecondSlot;
            _inputService.SelectSlot3.Performed += SelectThirdSlot;
            _inputService.SelectSlot4.Performed += SelectFourthSlot;
            _inputService.SelectSlotScroll.VectorChangedPerformed += CycleSlots;
            _inputService.Release.Started += BeginRelease;
            _inputService.Release.Canceled += EndRelease;
        }

        public override void OnStopLocalPlayer() {
            _inputService.SelectSlot1.Performed -= SelectFirstSlot;
            _inputService.SelectSlot2.Performed -= SelectSecondSlot;
            _inputService.SelectSlot3.Performed -= SelectThirdSlot;
            _inputService.SelectSlot4.Performed -= SelectFourthSlot;
            _inputService.SelectSlotScroll.VectorChangedPerformed -= CycleSlots;
            _inputService.Release.Started -= BeginRelease;
            _inputService.Release.Canceled -= EndRelease;
            _releasePress.Cancel();
        }

        private void Update() {
            if (isLocalPlayer == false || _releasePress.IsPressed == false)
                return;

            if (IsInputAllowed() == false || _grab.IsHolding == false) {
                _releasePress.Cancel();
                return;
            }

            if (_releasePress.TryCompleteHold(Time.time))
                _inventory.RequestThrow(_gameCameraService.OutputCamera.transform.forward);
        }

        private bool IsInputAllowed() =>
            _playerControlBlockModel.IsBlocked == false && _movable.IsHeld == false && _movable.IsKnockedDown == false;

        private void SelectFirstSlot() =>
            SelectSlot(FIRST_SLOT);

        private void SelectSecondSlot() =>
            SelectSlot(SECOND_SLOT);

        private void SelectThirdSlot() =>
            SelectSlot(THIRD_SLOT);

        private void SelectFourthSlot() =>
            SelectSlot(FOURTH_SLOT);

        private void SelectSlot(int slot) {
            if (IsInputAllowed())
                _inventory.RequestSelect(slot);
        }

        // One slot per wheel notch: scrolling down goes to the next slot, up to the previous one.
        private void CycleSlots(Vector2 scroll) {
            if (IsInputAllowed() == false || Mathf.Approximately(scroll.y, 0f))
                return;

            _inventory.RequestCycle(scroll.y < 0f ? 1 : -1);
        }

        private void BeginRelease() {
            if (IsInputAllowed() && _grab.IsHolding)
                _releasePress.Press(Time.time);
        }

        private void EndRelease() {
            if (_releasePress.TryEndAsTap())
                _interaction.ReleaseHeld();
        }
    }
}
