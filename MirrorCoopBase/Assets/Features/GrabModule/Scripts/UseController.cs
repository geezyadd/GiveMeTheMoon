using Features.CameraModule.Scripts.Services;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    public sealed class UseController : NetworkBehaviour {
        [SerializeField] private GrabController _grab;
        [SerializeField] private LayerMask _interactableMask;
        [SerializeField] private float _range = 4f;

        [Inject]
        private IInputService _input;

        [Inject]
        private IGameCameraService _cameras;

        private Collider _hoverCollider;
        private InteractableBase[] _onTarget = System.Array.Empty<InteractableBase>();
        private InteractableBase _hovered;

        public override void OnStartLocalPlayer() {
            _input.Grab.Performed += OnUse;
        }

        public override void OnStopLocalPlayer() {
            if (_input != null)
                _input.Grab.Performed -= OnUse;

            SetHovered(null);
        }

        private void Update() {
            if (isLocalPlayer == false)
                return;

            RefreshHover();
        }

        private void OnUse() {
            InteractableBase target = ResolveUsable();
            if (target == null)
                return;

            CmdUse(target.netId);
        }

        [Command]
        private void CmdUse(uint targetNetId) {
            if (targetNetId == 0 || NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity) == false)
                return;

            if (IsInReach(identity.transform.position) == false)
                return;

            InteractableBase[] targets = identity.GetComponents<InteractableBase>();
            for (int i = 0; i < targets.Length; i++) {
                InteractableBase target = targets[i];
                if (target == null || target.CanUse(netIdentity, _grab) == false)
                    continue;

                target.ServerUse(netIdentity, _grab);
                return;
            }
        }

        private void RefreshHover() {
            SetHovered(ResolveUsable());
        }

        private InteractableBase ResolveUsable() {
            if (TryRaycastHit(out RaycastHit hit) == false) {
                _hoverCollider = null;
                _onTarget = System.Array.Empty<InteractableBase>();
                return null;
            }

            if (hit.collider != _hoverCollider) {
                _hoverCollider = hit.collider;
                _onTarget = hit.collider.GetComponentsInParent<InteractableBase>(true);
            }

            for (int i = 0; i < _onTarget.Length; i++) {
                InteractableBase target = _onTarget[i];
                if (target != null && target.CanUse(netIdentity, _grab))
                    return target;
            }

            return null;
        }

        private void SetHovered(InteractableBase next) {
            if (_hovered == next)
                return;

            if (_hovered != null)
                _hovered.SetHovered(false);

            _hovered = next;
            if (_hovered != null)
                _hovered.SetHovered(true);
        }

        private bool IsInReach(Vector3 worldPosition) {
            Vector3 from = transform.position + Vector3.up;
            float reach = _range + 1.5f;
            return (worldPosition - from).sqrMagnitude <= reach * reach;
        }

        private bool TryRaycastHit(out RaycastHit hit) {
            hit = default;
            Camera camera = _cameras != null ? _cameras.OutputCamera : null;
            if (camera == null)
                return false;

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            return Physics.Raycast(ray, out hit, _range, _interactableMask, QueryTriggerInteraction.Collide);
        }
    }
}
