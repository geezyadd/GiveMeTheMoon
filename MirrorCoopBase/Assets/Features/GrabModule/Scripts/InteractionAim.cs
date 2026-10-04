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

        public InteractionAim(IGameCameraService cameras, GrabConfiguration configuration) {
            _cameras = cameras;
            _configuration = configuration;
        }

        public bool TryGetLookedAt(Transform player, Vector3 eye, out Grabbable grabbable, out InteractableBase[] interactables) {
            grabbable = null;
            interactables = Array.Empty<InteractableBase>();
            if (TryFindTargetCollider(player, eye, out Collider target) == false)
                return false;

            if (target != _cachedCollider) {
                _cachedCollider = target;
                _cachedGrabbable = target.GetComponentInParent<Grabbable>();
                _cachedInteractables = target.GetComponentsInParent<InteractableBase>(true);
            }

            grabbable = _cachedGrabbable;
            interactables = _cachedInteractables;
            return true;
        }

        private bool TryFindTargetCollider(Transform player, Vector3 eye, out Collider target) {
            target = null;
            Camera camera = _cameras.OutputCamera;
            if (camera == null)
                return false;

            Transform view = camera.transform;
            Vector3 direction = view.forward;
            float behindEye = Mathf.Max(0f, Vector3.Dot(eye - view.position, direction));
            Vector3 origin = view.position + direction * behindEye;
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
