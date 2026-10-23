using UnityEngine;

namespace Features.LobbyModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(ShipHullHudConfiguration) + "_Default",
        menuName = "Configurations/LobbyModule/" + nameof(ShipHullHudConfiguration))]
    public sealed class ShipHullHudConfiguration : ScriptableObject {
        [Tooltip("How long the hull bar's flash fades out after a hit, in seconds.")]
        [SerializeField, Min(0.01f)] private float _flashSeconds = 0.35f;
        [Tooltip("The flash overlay's opacity right after a hit.")]
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.85f;

        public float FlashSeconds => _flashSeconds;
        public float FlashAlpha => _flashAlpha;
    }
}
