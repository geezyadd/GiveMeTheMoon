using UnityEngine;

namespace Features.SonarModule.Contracts {
    // What a sonar contact is: its look on the scope and in the world, and whether the navigator may mark it. A new
    // kind of contact (an enemy) is a new asset, never a branch on the kind.
    [CreateAssetMenu(fileName = nameof(SonarContactKind), menuName = "Configurations/Sonar/" + nameof(SonarContactKind))]
    public sealed class SonarContactKind : ScriptableObject {
        [Tooltip("Stable id sent over the network with a mark: never rename it once it is used.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Sprite _icon;
        [Tooltip("The blip on the scope and the marker on the screen, in pixels.")]
        [SerializeField] private float _iconPixels = 24f;
        [Tooltip("How long a mark of this kind stays in the world.")]
        [SerializeField] private float _markSeconds = 20f;
        [SerializeField] private bool _isMarkable = true;
        [Tooltip("Shown on the scope's edge when it lies beyond the sonar range (the station), instead of not at all.")]
        [SerializeField] private bool _isShownBeyondRange;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Color Color => _color;
        public Sprite Icon => _icon;
        public float IconPixels => _iconPixels;
        public float MarkSeconds => _markSeconds;
        public bool IsMarkable => _isMarkable;
        public bool IsShownBeyondRange => _isShownBeyondRange;
    }
}
