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
        [Tooltip("How long a module that just broke blinks on the ship panel, in seconds.")]
        [SerializeField, Min(0f)] private float _brokenBlinkSeconds = 2.5f;
        [Tooltip("Blinks per second of a module that just broke.")]
        [SerializeField, Min(0.1f)] private float _brokenBlinksPerSecond = 4f;

        public float FlashSeconds => _flashSeconds;
        public float FlashAlpha => _flashAlpha;
        public float BrokenBlinkSeconds => _brokenBlinkSeconds;
        public float BrokenBlinksPerSecond => _brokenBlinksPerSecond;
    }
}
