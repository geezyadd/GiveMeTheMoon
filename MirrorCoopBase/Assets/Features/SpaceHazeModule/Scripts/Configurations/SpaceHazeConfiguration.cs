using UnityEngine;

namespace Features.SpaceHazeModule.Scripts.Configurations {
    // The dark space haze every peer draws over the world: what lies within the clear distance is seen sharp, past
    // it a thing fades into the backdrop behind it (the sky and the planets) and is gone at the hidden distance; the
    // backdrop beyond the backdrop distances stays visible.
    [CreateAssetMenu(fileName = nameof(SpaceHazeConfiguration) + "_Default", menuName = "Configurations/SpaceHaze/" + nameof(SpaceHazeConfiguration))]
    public sealed class SpaceHazeConfiguration : ScriptableObject {
        [SerializeField] private Shader _shader;
        [SerializeField] private bool _isEnabled = true;

        [Header("Distance")]
        [Tooltip("Within this distance from the camera nothing is hazed, in metres.")]
        [SerializeField] private float _clearMeters = 150f;
        [Tooltip("From here on a thing is hidden in the haze, in metres; between the clear and this distance it fades.")]
        [SerializeField] private float _hiddenMeters = 260f;
        [Tooltip("The haze thins out again from this distance, so the backdrop (planets, 2 km and farther) stays visible.")]
        [SerializeField] private float _backdropStartMeters = 1800f;
        [Tooltip("From this distance on nothing is hazed, and a thing wholly past it is part of the backdrop a hazed thing fades into; must stay below the camera's far clip, where the sky is.")]
        [SerializeField] private float _backdropEndMeters = 2000f;

        [Header("Look")]
        [Tooltip("1: a hidden thing is fully replaced by the backdrop behind it (the sky and the planets); less lets its shape show through.")]
        [Range(0f, 1f)]
        [SerializeField] private float _strength = 1f;
        [Tooltip("The haze's own tint; its alpha is how much of it lies over the backdrop behind a hazed thing.")]
        [SerializeField] private Color _tint = new(0.05f, 0.03f, 0.09f, 0.15f);

        public Shader Shader => _shader;
        public bool IsEnabled => _isEnabled;
        public float ClearMeters => _clearMeters;
        public float HiddenMeters => _hiddenMeters;
        public float BackdropStartMeters => _backdropStartMeters;
        public float BackdropEndMeters => _backdropEndMeters;
        public float Strength => _strength;
        public Color Tint => _tint;
    }
}
