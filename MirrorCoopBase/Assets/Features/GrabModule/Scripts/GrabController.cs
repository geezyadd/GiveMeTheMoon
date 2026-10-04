using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    public sealed class GrabController : NetworkBehaviour, IHeldItemRelease {
        [SerializeField] private Transform _armPoint;
        [SerializeField] private LayerMask _interactableMask;
        [SerializeField] private float _range = 4f;

        [Inject]
        private IInputService _input;

        [Inject]
        private IGameCameraService _cameras;

        [Inject]
        private HoveredInteractableModel _hoveredInteractableModel;

        [Inject]
        private PlayerControlBlockModel _controlBlock;

        [SyncVar(hook = nameof(OnHeldNetIdChanged))]
        private uint _heldNetId;

        private Grabbable _held;
        private Collider _hoverCollider;
        private Grabbable _cachedOnCollider;
        private Grabbable _hovered;

        public Transform ArmPoint => _armPoint != null ? _armPoint : transform;
        public bool IsHolding => _heldNetId != 0;
        public Grabbable Held => _held;

        public override void OnStartLocalPlayer() {
            _input.Grab.Performed += OnGrab;
            _input.Release.Performed += OnRelease;
        }

        public override void OnStopLocalPlayer() {
            if (_input != null) {
                _input.Grab.Performed -= OnGrab;
                _input.Release.Performed -= OnRelease;
            }

            SetHovered(null);
            _hoveredInteractableModel.SetGrabbable(null);
            // On disconnect the connection is already gone; OnStopServer releases the held item on the server side.
            if (isOwned && NetworkClient.isConnected)
                CmdRelease();
        }

        public override void OnStopServer() {
            ReleaseHeld();
        }

        [Server]
        public void ServerReleaseHeld() =>
            ReleaseHeld();

        public void ServerConsumeHeld() {
            if (isServer == false || _held == null)
                return;

            Grabbable held = _held;
            SetHeld(null);
            NetworkServer.Destroy(held.gameObject);
        }

        public bool ServerGive(Grabbable item) {
            if (isServer == false || item == null || _held != null)
                return false;

            item.ServerBind(netId);
            SetHeld(item);
            return true;
        }

        private void Update() {
            // The held item can be destroyed under the hand (e.g. the lobby unloads on Start): drop the stale hold.
            if (isServer && _heldNetId != 0 && _held == null)
                SetHeld(null);

            if (isLocalPlayer == false)
                return;

            RefreshHover();
        }

        private void OnGrab() {
            if (_controlBlock.IsBlocked)
                return;

            Grabbable item = ResolveGrabbable();
            if (item == null || item.CanBeGrabbed == false || IsHolding)
                return;

            CmdTryGrab(item.netId);
        }

        private void OnRelease() {
            if (_controlBlock.IsBlocked)
                return;

            CmdRelease();
        }

        [Command]
        private void CmdTryGrab(uint itemNetId) {
            if (_held != null || itemNetId == 0)
                return;

            if (NetworkServer.spawned.TryGetValue(itemNetId, out NetworkIdentity identity) == false)
                return;

            Grabbable item = identity.GetComponent<Grabbable>();
            if (item == null || item.CanBeGrabbed == false)
                return;

            if (IsInReach(item.transform.position) == false)
                return;

            item.ServerBind(netId);
            SetHeld(item);
        }

        [Command]
        private void CmdRelease() {
            ReleaseHeld();
        }

        private void RefreshHover() {
            if (IsHolding || _controlBlock.IsBlocked) {
                SetHovered(null);
                _hoverCollider = null;
                _cachedOnCollider = null;
                _hoveredInteractableModel.SetGrabbable(null);
                return;
            }

            Grabbable next = ResolveGrabbable();
            SetHovered(next);
            _hoveredInteractableModel.SetGrabbable(next);
        }

        private Grabbable ResolveGrabbable() {
            if (TryRaycastHit(out RaycastHit hit) == false) {
                _hoverCollider = null;
                _cachedOnCollider = null;
                return null;
            }

            if (hit.collider != _hoverCollider) {
                _hoverCollider = hit.collider;
                _cachedOnCollider = hit.collider.GetComponentInParent<Grabbable>();
            }

            return _cachedOnCollider != null && _cachedOnCollider.CanBeGrabbed ? _cachedOnCollider : null;
        }

        private void SetHovered(Grabbable next) {
            if (_hovered == next)
                return;

            if (_hovered != null)
                _hovered.SetHovered(false);

            _hovered = next;
            if (_hovered != null)
                _hovered.SetHovered(true);
        }

        private void ReleaseHeld() {
            if (_held != null)
                _held.ServerUnbind();

            SetHeld(null);
        }

        private void SetHeld(Grabbable item) {
            _held = item;
            _heldNetId = item != null ? item.netId : 0u;
        }

        private void OnHeldNetIdChanged(uint previous, uint current) {
            if (current == 0) {
                _held = null;
                return;
            }

            if (TryResolveGrabbable(current, out Grabbable item) == false) {
                _held = null;
                return;
            }

            _held = item;
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

        private static bool TryResolveGrabbable(uint itemNetId, out Grabbable item) {
            item = null;
            NetworkIdentity identity = null;
            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(itemNetId, out identity) == false)
                identity = null;
            if (identity == null && NetworkClient.active && NetworkClient.spawned.TryGetValue(itemNetId, out identity) == false)
                return false;
            if (identity == null)
                return false;

            item = identity.GetComponent<Grabbable>();
            return item != null;
        }
    }
}
