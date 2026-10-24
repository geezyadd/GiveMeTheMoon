using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts.Hold;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // The local player's side of interaction: one aim per frame, one highlight, and one handler for the interact
    // button, so a press either uses an interactable or grabs an item, never both. A hold target is held until the
    // button is released or the crosshair leaves it.
    public sealed class LocalPlayerInteraction : NetworkBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private UseController _use;
        [SerializeField] private Transform _aimOrigin;

        private IInputService _input;
        private PlayerControlBlockModel _controlBlock;
        private HoldUseModel _holdUseModel;
        private InteractionAim _aim;
        private InteractionHighlight _highlight;
        private InteractionTarget _target;

        public InteractionTarget Target => _target;

        private Vector3 Eye => _aimOrigin != null ? _aimOrigin.position : transform.position;

        [Inject]
        private void InjectDependencies(
            IInputService input,
            IGameCameraService cameras,
            GrabConfiguration configuration,
            HoveredInteractableModel hoveredModel,
            PlayerControlBlockModel controlBlock,
            HoldUseModel holdUseModel) {
            _input = input;
            _controlBlock = controlBlock;
            _holdUseModel = holdUseModel;
            _aim = new InteractionAim(cameras, configuration);
            _highlight = new InteractionHighlight(hoveredModel);
        }

        public override void OnStartLocalPlayer() {
            _input.Grab.Performed += Interact;
            _input.Release.Performed += ReleaseHeld;
            _input.Grab.Canceled += EndInteract;
            _use.OnHoldProgressChanged += OnHoldProgressChanged;
        }

        public override void OnStopLocalPlayer() {
            _input.Grab.Performed -= Interact;
            _input.Release.Performed -= ReleaseHeld;
            _input.Grab.Canceled -= EndInteract;
            _use.OnHoldProgressChanged -= OnHoldProgressChanged;
            _use.RequestEndHold();
            _holdUseModel.SetProgress(0f);
            _target = default;
            _highlight.Clear();
        }

        public void Interact() {
            if (_controlBlock.IsBlocked)
                return;

            if (_target.Usable != null)
                _use.RequestUse(_target.Usable);
            else if (_target.Grabbable != null)
                _grab.RequestGrab(_target.Grabbable);
        }

        public void EndInteract() =>
            _use.RequestEndHold();

        public void ReleaseHeld() {
            if (_controlBlock.IsBlocked == false)
                _grab.RequestRelease();
        }

        private void Update() {
            if (isLocalPlayer == false)
                return;

            _target = FindTarget();
            _highlight.Show(_target);
            if (IsHoldTargetLost())
                _use.RequestEndHold();
        }

        private bool IsHoldTargetLost() =>
            _use.RequestedHoldTarget != null && _use.RequestedHoldTarget != _target.Usable;

        private InteractionTarget FindTarget() {
            if (_controlBlock.IsBlocked)
                return default;

            if (_aim.TryGetLookedAt(transform, Eye, out Grabbable grabbable, out InteractableBase[] interactables) == false)
                return default;

            InteractableBase usable = null;
            InteractableBase lookedAt = null;
            for (int i = 0; i < interactables.Length; i++) {
                InteractableBase interactable = interactables[i];
                if (interactable == null)
                    continue;

                if (lookedAt == null)
                    lookedAt = interactable;

                if (usable == null && _use.CanUse(interactable))
                    usable = interactable;
            }

            bool canGrab = grabbable != null && grabbable.CanBeGrabbed && _grab.IsHolding == false;
            return new InteractionTarget(canGrab ? grabbable : null, usable, lookedAt);
        }

        private void OnHoldProgressChanged() =>
            _holdUseModel.SetProgress(_use.HoldProgress);
    }
}
