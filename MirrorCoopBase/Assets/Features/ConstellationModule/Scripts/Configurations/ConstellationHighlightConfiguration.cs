using UnityEngine;

namespace Features.ConstellationModule.Scripts.Configurations {
    // The look of a highlighted constellation: a glow on each star and thin lines of the figure between them.
    [CreateAssetMenu(fileName = nameof(ConstellationHighlightConfiguration) + "_Default", menuName = "Configurations/Constellation/" + nameof(ConstellationHighlightConfiguration))]
    public sealed class ConstellationHighlightConfiguration : ScriptableObject {
        [Tooltip("Angular radius of a star's glow, in degrees.")]
        [SerializeField] private float _starRadiusDegrees = 0.6f;
        [Tooltip("Colour of a star's glow at its centre; HDR, above 1 the Bloom makes it shine.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _starColor = new(2.5f, 2.3f, 2f, 1f);
        [Tooltip("Colour of the figure's lines; HDR, keep it below 1 so the lines stay calm and do not bloom.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _lineColor = new(0.25f, 0.35f, 0.55f, 1f);
        [Tooltip("A line stops this far from the star at each of its ends, in degrees, so it does not run into the glow.")]
        [SerializeField] private float _lineGapDegrees = 0.9f;

        public float StarRadiusDegrees => _starRadiusDegrees;
        public Color StarColor => _starColor;
        public Color LineColor => _lineColor;
        public float LineGapDegrees => _lineGapDegrees;
    }
}
