using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Views {
    // Highlights one constellation over the skybox, on this peer only (nothing is synced). Runs in edit mode too, so
    // the figure can be checked against the painted stars in the Scene view; Zenject does not inject in edit mode,
    // hence the serialized data and the builder made here.
    [ExecuteAlways]
    public sealed class ConstellationHighlightView : MonoBehaviour {
        private const float STAR_PART = 0f;
        private const float LINE_PART = 1f;

        private static readonly int COLOR_PROPERTY = Shader.PropertyToID("_Color");
        private static readonly int IS_LINE_PROPERTY = Shader.PropertyToID("_IsLine");
        private static readonly int CORE_RADIUS_PROPERTY = Shader.PropertyToID("_CoreRadius");
        private static readonly int CORE_INTENSITY_PROPERTY = Shader.PropertyToID("_CoreIntensity");
        private static readonly int HALO_RADIUS_PROPERTY = Shader.PropertyToID("_HaloRadius");
        private static readonly int HALO_INTENSITY_PROPERTY = Shader.PropertyToID("_HaloIntensity");
        private static readonly int SPIKE_LENGTH_PROPERTY = Shader.PropertyToID("_SpikeLength");
        private static readonly int SPIKE_WIDTH_PROPERTY = Shader.PropertyToID("_SpikeWidth");
        private static readonly int SPIKE_INTENSITY_PROPERTY = Shader.PropertyToID("_SpikeIntensity");
        private static readonly int TWINKLE_AMOUNT_PROPERTY = Shader.PropertyToID("_TwinkleAmount");
        private static readonly int TWINKLE_SPEED_PROPERTY = Shader.PropertyToID("_TwinkleSpeed");
        private static readonly int LINE_WIDTH_PROPERTY = Shader.PropertyToID("_LineWidth");
        private static readonly int LINE_INTENSITY_PROPERTY = Shader.PropertyToID("_LineIntensity");
        private static readonly int LINE_GLOW_INTENSITY_PROPERTY = Shader.PropertyToID("_LineGlowIntensity");
        private static readonly int LINE_END_FADE_PROPERTY = Shader.PropertyToID("_LineEndFade");

        [SerializeField] private ConstellationDefinition _constellation;
        [SerializeField] private CelestialSkyFrameConfiguration _celestialSkyFrameConfiguration;
        [SerializeField] private ConstellationHighlightConfiguration _constellationHighlightConfiguration;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;

        private Mesh _mesh;

        private void OnEnable() {
            ConstellationMeshBuilder builder = new(_celestialSkyFrameConfiguration, _constellationHighlightConfiguration);
            _mesh = builder.Build(_constellation);
            _meshFilter.sharedMesh = _mesh;
            ApplyStarLook();
            ApplyLineLook();
        }

        private void OnDisable() {
            _meshFilter.sharedMesh = null;
            DestroyImmediate(_mesh);
        }

        private void ApplyStarLook() {
            MaterialPropertyBlock block = new();
            block.SetFloat(IS_LINE_PROPERTY, STAR_PART);
            block.SetColor(COLOR_PROPERTY, _constellationHighlightConfiguration.StarColor);
            block.SetFloat(CORE_RADIUS_PROPERTY, _constellationHighlightConfiguration.CoreRadiusDegrees);
            block.SetFloat(CORE_INTENSITY_PROPERTY, _constellationHighlightConfiguration.CoreIntensity);
            block.SetFloat(HALO_RADIUS_PROPERTY, _constellationHighlightConfiguration.HaloRadiusDegrees);
            block.SetFloat(HALO_INTENSITY_PROPERTY, _constellationHighlightConfiguration.HaloIntensity);
            block.SetFloat(SPIKE_LENGTH_PROPERTY, _constellationHighlightConfiguration.SpikeLengthDegrees);
            block.SetFloat(SPIKE_WIDTH_PROPERTY, _constellationHighlightConfiguration.SpikeWidthDegrees);
            block.SetFloat(SPIKE_INTENSITY_PROPERTY, _constellationHighlightConfiguration.SpikeIntensity);
            block.SetFloat(TWINKLE_AMOUNT_PROPERTY, _constellationHighlightConfiguration.TwinkleAmount);
            block.SetFloat(TWINKLE_SPEED_PROPERTY, _constellationHighlightConfiguration.TwinkleSpeed);
            _meshRenderer.SetPropertyBlock(block, ConstellationMeshBuilder.STAR_SUBMESH);
        }

        private void ApplyLineLook() {
            MaterialPropertyBlock block = new();
            block.SetFloat(IS_LINE_PROPERTY, LINE_PART);
            block.SetColor(COLOR_PROPERTY, _constellationHighlightConfiguration.LineColor);
            block.SetFloat(LINE_WIDTH_PROPERTY, _constellationHighlightConfiguration.LineWidthPixels);
            block.SetFloat(LINE_INTENSITY_PROPERTY, _constellationHighlightConfiguration.LineIntensity);
            block.SetFloat(LINE_GLOW_INTENSITY_PROPERTY, _constellationHighlightConfiguration.LineGlowIntensity);
            block.SetFloat(LINE_END_FADE_PROPERTY, _constellationHighlightConfiguration.LineEndFadeDegrees);
            _meshRenderer.SetPropertyBlock(block, ConstellationMeshBuilder.LINE_SUBMESH);
        }
    }
}
