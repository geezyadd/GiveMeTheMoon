using UnityEngine;

namespace Features.PlayerProfileModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(PlayerNameplateConfiguration) + "_Default",
        menuName = "Configurations/PlayerProfileModule/" + nameof(PlayerNameplateConfiguration))]
    public sealed class PlayerNameplateConfiguration : ScriptableObject {
        [Tooltip("Longest player name in characters, the ellipsis included. Longer names are cut and end with an ellipsis.")]
        [SerializeField, Min(2)] private int _maxNameLength = 24;

        [Tooltip("Shown with the player's number when the name is empty after cleanup.")]
        [SerializeField] private string _fallbackNamePrefix = "Player ";

        [Tooltip("Height of the nameplate above the player's feet, in meters.")]
        [SerializeField, Min(0f)] private float _heightAboveFeet = 2.3f;

        [Tooltip("Font size of the nameplate at scale 1. A font size of 10 is one meter.")]
        [SerializeField, Min(0.01f)] private float _fontSize = 1.5f;

        [Tooltip("Up to this distance the nameplate keeps its world size, so it does not grow huge up close.")]
        [SerializeField, Min(0.01f)] private float _constantScreenSizeFromDistance = 5f;

        [Tooltip("From the near distance up to this one the nameplate keeps its size on screen; farther it shrinks with distance.")]
        [SerializeField, Min(0.01f)] private float _constantScreenSizeToDistance = 30f;

        [Tooltip("Distance where the nameplate starts to fade out.")]
        [SerializeField, Min(0f)] private float _fadeStartDistance = 30f;

        [Tooltip("Distance where the nameplate is fully faded out and hidden.")]
        [SerializeField, Min(0f)] private float _fadeEndDistance = 35f;

        public int MaxNameLength =>
            _maxNameLength;

        public string FallbackNamePrefix =>
            _fallbackNamePrefix;

        public float HeightAboveFeet =>
            _heightAboveFeet;

        public float FontSize =>
            _fontSize;

        public float ConstantScreenSizeFromDistance =>
            _constantScreenSizeFromDistance;

        public float ConstantScreenSizeToDistance =>
            _constantScreenSizeToDistance;

        public float FadeStartDistance =>
            _fadeStartDistance;

        public float FadeEndDistance =>
            _fadeEndDistance;
    }
}
