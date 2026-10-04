using UnityEngine;

namespace Features.GrabModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(GrabConfiguration) + "_Default",
        menuName = "Configurations/GrabModule/" + nameof(GrabConfiguration))]
    public sealed class GrabConfiguration : ScriptableObject {
        [Tooltip("How far the player reaches, measured from the eye along the view, not from the camera.")]
        [SerializeField, Min(0f)] private float _aimRange = 4f;

        [Tooltip("Layers of the things the player can grab or use.")]
        [SerializeField] private LayerMask _interactableMask = 1 << 7;

        [Tooltip("Solid layers that block the aim, so nothing is grabbed or used through a wall or the deck.")]
        [SerializeField] private LayerMask _aimBlockerMask = (1 << 0) | (1 << 6);

        [Tooltip("Height above the player's feet the server measures reach from.")]
        [SerializeField, Min(0f)] private float _serverReachOriginHeight = 1f;

        [Tooltip("Extra reach the server allows on top of the aim range, for latency and the target's size.")]
        [SerializeField, Min(0f)] private float _serverReachSlack = 1.5f;

        public float AimRange =>
            _aimRange;

        public LayerMask InteractableMask =>
            _interactableMask;

        public LayerMask AimBlockerMask =>
            _aimBlockerMask;

        public float ServerReachOriginHeight =>
            _serverReachOriginHeight;

        public float ServerReachSlack =>
            _serverReachSlack;
    }
}
