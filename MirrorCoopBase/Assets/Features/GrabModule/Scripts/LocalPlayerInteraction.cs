using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // The local player's side of interaction: one aim per frame, one highlight, and one handler for the interact
    // button, so a press either uses an interactable or grabs an item, never both.
    public sealed class LocalPlayerInteraction : NetworkBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private UseController _use;
        [SerializeField] private Transform _aimOrigin;

        private IInputService _input;
        private PlayerControlBlockModel _controlBlock;
        private InteractionAim _aim;
        private InteractionHighlight _highlight;
        private InteractionTarget _target;

        public InteractionTarget Target => _target;

        private Vector3 Eye => _aimOrigin != null ? _aimOrigin.position : transform.position;

        [Inject]
        private void Construct(
            IInputService input,
            IGameCameraService cameras,
            GrabConfiguration configuration,
            HoveredInteractableModel hoveredModel,
            PlayerControlBlockModel controlBlock) {
            _input = input;
            _controlBlock = controlBlock;
            _aim = new InteractionAim(cameras, configuration);
            _highlight = new InteractionHighlight(hoveredModel);
        }

        public override void OnStartLocalPlayer() {
            _input.Grab.Performed += Interact;
            _input.Release.Performed += ReleaseHeld;
        }

        public override void OnStopLocalPlayer() {
            _input.Grab.Performed -= Interact;
            _input.Release.Performed -= ReleaseHeld;
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

        public void ReleaseHeld() {
            if (_controlBlock.IsBlocked == false)
                _grab.RequestRelease();
        }

        private void Update() {
            if (isLocalPlayer == false)
                return;

            _target = FindTarget();
            _highlight.Show(_target);
        }

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
    }
}
