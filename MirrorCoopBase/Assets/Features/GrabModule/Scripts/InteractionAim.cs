using System;
using Features.CameraModule.Scripts.Services;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    // Finds what the camera looks at within the player's reach. The ray goes along the camera view but starts level
    // with the player's eye, so the third-person camera behind the player does not eat the reach.
    public sealed class InteractionAim {
        private const int MAX_HITS = 16;

        private readonly IGameCameraService _cameras;
        private readonly GrabConfiguration _configuration;
        private readonly RaycastHit[] _hits = new RaycastHit[MAX_HITS];

        private Collider _cachedCollider;
        private Grabbable _cachedGrabbable;
        private InteractableBase[] _cachedInteractables = Array.Empty<InteractableBase>();
        private Transform _cachedPlayer;
        private Rigidbody _cachedPlayerBody;
        private float _lastHitTime = float.NegativeInfinity;

        public InteractionAim(IGameCameraService cameras, GrabConfiguration configuration) {
            _cameras = cameras;
            _configuration = configuration;
        }

        public bool TryGetLookedAt(Transform player, Vector3 eye, out Grabbable grabbable, out InteractableBase[] interactables) {
            grabbable = null;
            interactables = Array.Empty<InteractableBase>();
            if (TryFindTargetCollider(player, eye, out Collider target) == false)
                return TryHoldLastTarget(out grabbable, out interactables);

            _lastHitTime = Time.unscaledTime;

            if (target != _cachedCollider) {
                _cachedCollider = target;
                _cachedGrabbable = target.GetComponentInParent<Grabbable>();
                _cachedInteractables = target.GetComponentsInParent<InteractableBase>(true);
            }

            grabbable = _cachedGrabbable;
            interactables = _cachedInteractables;
            return true;
        }

        // A ray that slipped off the target it was on a moment ago keeps it for the hold time: on a moving deck items
        // follow the ship's drawn pose while ship parts follow its physics pose, and one frame's miss made the outline
        // flicker. A target that is gone or switched off is not held.
        private bool TryHoldLastTarget(out Grabbable grabbable, out InteractableBase[] interactables) {
            grabbable = null;
            interactables = Array.Empty<InteractableBase>();
            if (_cachedCollider == null || _cachedCollider.enabled == false || _cachedCollider.gameObject.activeInHierarchy == false)
                return false;

            if (Time.unscaledTime - _lastHitTime > _configuration.AimHoldSeconds)
                return false;

            grabbable = _cachedGrabbable;
            interactables = _cachedInteractables;
            return true;
        }

        // How far the player's physical body is ahead of where it is drawn. A moving body (on a flying ship or platform)
        // is drawn interpolated, a fraction of a physics step behind its physics pose, and so is the ship around it,
        // while the colliders the ray hits are at their physics poses: a ray from the drawn camera missed the deck's
        // items by up to a step of the ship's motion, more or less from frame to frame, and their outline flickered.
        // Moved by this lag, the ray is cast where the physics has the player, among the colliders it looks at.
        private Vector3 DrawLag(Transform player) {
            if (player != _cachedPlayer) {
                _cachedPlayer = player;
                _cachedPlayerBody = player != null ? player.GetComponent<Rigidbody>() : null;
            }

            if (_cachedPlayerBody == null || _cachedPlayerBody.interpolation == RigidbodyInterpolation.None)
                return Vector3.zero;

            return _cachedPlayerBody.position - player.position;
        }

        private bool TryFindTargetCollider(Transform player, Vector3 eye, out Collider target) {
            target = null;
            Camera camera = _cameras.OutputCamera;
            if (camera == null)
                return false;

            Transform view = camera.transform;
            Vector3 direction = view.forward;
            float behindEye = Mathf.Max(0f, Vector3.Dot(eye - view.position, direction));
            Vector3 origin = view.position + direction * behindEye + DrawLag(player);
            int interactableMask = _configuration.InteractableMask;
            int blockerMask = _configuration.AimBlockerMask & ~interactableMask;
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                _hits,
                _configuration.AimRange,
                interactableMask | blockerMask,
                QueryTriggerInteraction.Collide);

            float nearestBlocker = float.MaxValue;
            float nearestTarget = float.MaxValue;
            for (int i = 0; i < count; i++) {
                RaycastHit hit = _hits[i];
                Collider collider = hit.collider;
                if (collider.transform.IsChildOf(player))
                    continue;

                if (IsInMask(collider, interactableMask)) {
                    if (hit.distance < nearestTarget) {
                        nearestTarget = hit.distance;
                        target = collider;
                    }
                } else if (collider.isTrigger == false && hit.distance < nearestBlocker) {
                    nearestBlocker = hit.distance;
                }
            }

            return target != null && nearestTarget <= nearestBlocker;
        }

        private static bool IsInMask(Collider collider, int mask) =>
            (mask & (1 << collider.gameObject.layer)) != 0;
    }
}
