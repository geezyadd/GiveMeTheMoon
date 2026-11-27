using UnityEngine;

namespace Features.ConstellationModule.Scripts.Configurations {
    // The look of a highlighted constellation: each star like a bright star photographed through a telescope (a small
    // core, a soft halo, four thin diffraction spikes) and glowing lines of the figure between them. Star sizes are
    // given for a star of the reference magnitude; brighter stars are drawn larger and brighter, fainter ones smaller.
    [CreateAssetMenu(fileName = nameof(ConstellationHighlightConfiguration) + "_Default", menuName = "Configurations/Constellation/" + nameof(ConstellationHighlightConfiguration))]
    public sealed class ConstellationHighlightConfiguration : ScriptableObject {
        [Header("Magnitude")]
        [Tooltip("A star of this visual magnitude is drawn at the sizes and brightness below.")]
        [SerializeField] private float _referenceMagnitude = 1f;
        [Tooltip("How strongly the drawn size follows the star's light: size = flux ^ this (0.5 = size grows with the square root of the light).")]
        [SerializeField] private float _magnitudeSizeExponent = 0.5f;
        [Tooltip("How strongly the drawn brightness follows the star's light: brightness = flux ^ this.")]
        [SerializeField] private float _magnitudeBrightnessExponent = 0.5f;
        [Tooltip("Smallest and largest size factor a star may get from its magnitude.")]
        [SerializeField] private Vector2 _starScaleRange = new(0.35f, 2f);
        [Tooltip("Smallest and largest brightness factor a star may get from its magnitude.")]
        [SerializeField] private Vector2 _starBrightnessRange = new(0.35f, 2f);

        [Header("Star")]
        [Tooltip("Tint of every layer of a star; the layers' intensities below set how bright it is.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _starColor = new(0.95f, 0.97f, 1f, 1f);
        [Tooltip("Angular radius of the bright core (Gaussian width), in degrees.")]
        [SerializeField] private float _coreRadiusDegrees = 0.04f;
        [Tooltip("Brightness of the core's centre; well above 1, so the scene's Bloom (threshold 1) gives it a tight glow.")]
        [SerializeField] private float _coreIntensity = 8f;
        [Tooltip("Angular radius at which the soft halo fades out, in degrees.")]
        [SerializeField] private float _haloRadiusDegrees = 0.55f;
        [Tooltip("Brightness of the halo's centre; keep it low so the star stays a point of light, not a disc.")]
        [SerializeField] private float _haloIntensity = 0.3f;
        [Tooltip("Length of each of the four diffraction spikes from the star's centre, in degrees.")]
        [SerializeField] private float _spikeLengthDegrees = 1.4f;
        [Tooltip("Width of a spike at the star's centre, in degrees; it tapers to nothing at the spike's end.")]
        [SerializeField] private float _spikeWidthDegrees = 0.015f;
        [Tooltip("Brightness of a spike at the star's centre; it fades towards the spike's end.")]
        [SerializeField] private float _spikeIntensity = 1.6f;
        [Tooltip("Turn of the spike cross from an upright '+', in degrees, clockwise on screen.")]
        [SerializeField] private float _spikeAngleDegrees = 15f;
        [Tooltip("Share by which the halo's brightness slowly breathes up and down (0 = no twinkle).")]
        [Range(0f, 1f)]
        [SerializeField] private float _twinkleAmount = 0.08f;
        [Tooltip("Speed of the halo's twinkle, in radians per second.")]
        [SerializeField] private float _twinkleSpeed = 0.7f;

        [Header("Line")]
        [Tooltip("Tint of the figure's lines; the line intensity below sets how bright they are.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _lineColor = new(0.45f, 0.62f, 1f, 1f);
        [Tooltip("Full width of a line's glowing strip on screen, in pixels; the strip fades softly towards its edges.")]
        [SerializeField] private float _lineWidthPixels = 8f;
        [Tooltip("Brightness of a line's thin core; a little above 1, so the Bloom gives the line a faint halo.")]
        [SerializeField] private float _lineIntensity = 1.3f;
        [Tooltip("Brightness of the soft glow around a line's thin core, filling the whole strip.")]
        [SerializeField] private float _lineGlowIntensity = 0.18f;
        [Tooltip("A line stops this far from a reference-magnitude star, in degrees (scaled with the star's drawn size), so it does not run into the core.")]
        [SerializeField] private float _lineGapDegrees = 0.3f;
        [Tooltip("A line fades in over this angle at each of its ends, in degrees.")]
        [SerializeField] private float _lineEndFadeDegrees = 0.25f;

        public float ReferenceMagnitude => _referenceMagnitude;
        public float MagnitudeSizeExponent => _magnitudeSizeExponent;
        public float MagnitudeBrightnessExponent => _magnitudeBrightnessExponent;
        public Vector2 StarScaleRange => _starScaleRange;
        public Vector2 StarBrightnessRange => _starBrightnessRange;
        public Color StarColor => _starColor;
        public float CoreRadiusDegrees => _coreRadiusDegrees;
        public float CoreIntensity => _coreIntensity;
        public float HaloRadiusDegrees => _haloRadiusDegrees;
        public float HaloIntensity => _haloIntensity;
        public float SpikeLengthDegrees => _spikeLengthDegrees;
        public float SpikeWidthDegrees => _spikeWidthDegrees;
        public float SpikeIntensity => _spikeIntensity;
        public float SpikeAngleDegrees => _spikeAngleDegrees;
        public float TwinkleAmount => _twinkleAmount;
        public float TwinkleSpeed => _twinkleSpeed;
        public Color LineColor => _lineColor;
        public float LineWidthPixels => _lineWidthPixels;
        public float LineIntensity => _lineIntensity;
        public float LineGlowIntensity => _lineGlowIntensity;
        public float LineGapDegrees => _lineGapDegrees;
        public float LineEndFadeDegrees => _lineEndFadeDegrees;
    }
}
