using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts.Held;
using Features.GrabModule.Scripts.Hold;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // The local player's side of interaction: one aim per frame, one highlight, and one handler for the interact
    // button, so a press either uses an interactable or grabs an item, never both. A hold target is held until the
    // button is released or the crosshair leaves it. A held control is moved by the mouse, with the view locked, until
    // the button is released, the player is blocked or the server gives the control away.
    public sealed class LocalPlayerInteraction : NetworkBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private UseController _use;
        [SerializeField] private Transform _aimOrigin;

        private IInputService _input;
        private PlayerControlBlockModel _controlBlock;
        private HoldUseModel _holdUseModel;
        private CameraLookLockModel _cameraLookLockModel;
        private InteractionAim _aim;
        private InteractionHighlight _highlight;
        private InteractionTarget _target;
        private IHeldControlDriver _heldControl;
        private Vector2 _heldControlLook;

        public InteractionTarget Target => _target;

        private Vector3 Eye => _aimOrigin != null ? _aimOrigin.position : transform.position;

        [Inject]
        private void InjectDependencies(
            IInputService input,
            IGameCameraService cameras,
            GrabConfiguration configuration,
            HoveredInteractableModel hoveredModel,
            PlayerControlBlockModel controlBlock,
            HoldUseModel holdUseModel,
            CameraLookLockModel cameraLookLockModel) {
            _input = input;
            _controlBlock = controlBlock;
            _holdUseModel = holdUseModel;
            _cameraLookLockModel = cameraLookLockModel;
            _aim = new InteractionAim(cameras, configuration);
            _highlight = new InteractionHighlight(hoveredModel);
        }

        public override void OnStartLocalPlayer() {
            _input.Grab.Performed += Interact;
            _input.Release.Performed += ReleaseHeld;
            _input.Grab.Canceled += EndInteract;
            _input.Look.VectorChangedPerformed += MoveHeldControl;
            _use.OnHoldProgressChanged += OnHoldProgressChanged;
        }

        public override void OnStopLocalPlayer() {
            _input.Grab.Performed -= Interact;
            _input.Release.Performed -= ReleaseHeld;
            _input.Grab.Canceled -= EndInteract;
            _input.Look.VectorChangedPerformed -= MoveHeldControl;
            _use.OnHoldProgressChanged -= OnHoldProgressChanged;
            _use.RequestEndHold();
            EndHeldControl();
            _holdUseModel.SetProgress(0f);
            _target = default;
            _highlight.Clear();
        }

        public void Interact() {
            if (_controlBlock.IsBlocked)
                return;

            if (_target.Usable is IHeldControlDriver heldControl)
                BeginHeldControl(heldControl, _target.Usable);
            else if (_target.Usable != null)
                _use.RequestUse(_target.Usable);
            else if (_target.Grabbable != null)
                _grab.RequestGrab(_target.Grabbable);
        }

        public void EndInteract() {
            _use.RequestEndHold();
            EndHeldControl();
        }

        // The mouse's look input; while a control is held it moves the control instead of the view.
        public void MoveHeldControl(Vector2 look) {
            if (_heldControl != null)
                _heldControlLook += look;
        }

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

            DriveHeldControl();
        }

        private void BeginHeldControl(IHeldControlDriver heldControl, InteractableBase target) {
            EndHeldControl();
            _use.RequestUse(target);
            _heldControl = heldControl;
            _heldControlLook = Vector2.zero;
            _cameraLookLockModel.Request(this);
            heldControl.LocalBeginHold(netIdentity);
        }

        // The crosshair may leave a moving handle: a held control ends on release, a block or the server, not on aim.
        private void DriveHeldControl() {
            if (_heldControl == null)
                return;

            if (_controlBlock.IsBlocked || IsHeldControlGone()) {
                EndHeldControl();
                return;
            }

            HeldControlFrame frame = _heldControl.LocalDrive(_heldControlLook, Time.deltaTime);
            _heldControlLook = Vector2.zero;
            SendHeldControlPosition(frame);
            if (frame.IsLost)
                EndHeldControl();
        }

        private void EndHeldControl() {
            if (_heldControl == null)
                return;

            // A control destroyed with its map (the ship's throttle) has no last position to send.
            if (IsHeldControlGone() == false)
                SendHeldControlPosition(_heldControl.LocalEndHold());

            _heldControl = null;
            _heldControlLook = Vector2.zero;
            _cameraLookLockModel.Release(this);
            _use.RequestLetGoHeldControl();
        }

        private bool IsHeldControlGone() =>
            _heldControl is Object control && control == null;

        private void SendHeldControlPosition(HeldControlFrame frame) {
            if (frame.HasPositionToSend && frame.IsLost == false)
                _use.SendHeldControlPosition(frame.Position);
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
