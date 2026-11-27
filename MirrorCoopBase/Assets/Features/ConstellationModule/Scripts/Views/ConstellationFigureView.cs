using System.Collections.Generic;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;
using Zenject;

namespace Features.ConstellationModule.Scripts.Views {
    // Draws one or more constellation figures as one mesh: the star and line look per submesh (property blocks; _IsLine
    // picks the look in the shader), and every frame each star's light and each line's drawn share as the shader's
    // per-step array, in the mesh's figure order, times the whole drawing's alpha.
    public sealed class ConstellationFigureView : MonoBehaviour {
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
        private static readonly int STEP_PROGRESS_PROPERTY = Shader.PropertyToID("_StepProgress");
        private static readonly int FIGURE_ALPHA_PROPERTY = Shader.PropertyToID("_FigureAlpha");

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;

        private readonly float[] _starLights = new float[ConstellationMeshBuilder.MAX_REVEAL_STEPS];
        private readonly float[] _lineProgress = new float[ConstellationMeshBuilder.MAX_REVEAL_STEPS];
        private readonly List<ConstellationDefinition> _shownConstellations = new();

        private IConstellationMeshBuilder _constellationMeshBuilder;
        private ConstellationHighlightConfiguration _constellationHighlightConfiguration;
        private MaterialPropertyBlock _starBlock;
        private MaterialPropertyBlock _lineBlock;
        private Mesh _mesh;

        [Inject]
        private void InjectDependencies(IConstellationMeshBuilder constellationMeshBuilder, ConstellationHighlightConfiguration constellationHighlightConfiguration) {
            _constellationMeshBuilder = constellationMeshBuilder;
            _constellationHighlightConfiguration = constellationHighlightConfiguration;
        }

        // The renderer starts disabled in the scene: nothing is shown until a constellation is picked.
        private void Awake() {
            _starBlock = CreateStarLook();
            _lineBlock = CreateLineLook();
        }

        private void OnDestroy() =>
            DestroyMesh();

        public void Show(IReadOnlyList<ConstellationDefinition> constellations) {
            if (IsShowing(constellations))
                return;

            DestroyMesh();
            _mesh = _constellationMeshBuilder.Build(constellations);
            _meshFilter.sharedMesh = _mesh;
            _shownConstellations.Clear();
            _shownConstellations.AddRange(constellations);
            _meshRenderer.enabled = true;
        }

        public void Clear() {
            _meshRenderer.enabled = false;
            _shownConstellations.Clear();
            DestroyMesh();
        }

        // One timeline per shown figure, in the order the figures were shown; every figure is at the same reveal time.
        public void Draw(IReadOnlyList<ConstellationRevealTimeline> timelines, float revealSeconds, float alpha) {
            int firstStar = 0;
            int firstLine = 0;
            for (int i = 0; i < timelines.Count; i++) {
                FillProgress(timelines[i], revealSeconds, firstStar, firstLine);
                firstStar += timelines[i].StarCount;
                firstLine += timelines[i].LineCount;
            }
            ApplyProgress(_starBlock, _starLights, alpha, ConstellationMeshBuilder.STAR_SUBMESH);
            ApplyProgress(_lineBlock, _lineProgress, alpha, ConstellationMeshBuilder.LINE_SUBMESH);
        }

        private bool IsShowing(IReadOnlyList<ConstellationDefinition> constellations) {
            if (_shownConstellations.Count != constellations.Count)
                return false;
            for (int i = 0; i < constellations.Count; i++) {
                if (_shownConstellations[i] != constellations[i])
                    return false;
            }

            return true;
        }

        private void FillProgress(ConstellationRevealTimeline timeline, float revealSeconds, int firstStar, int firstLine) {
            for (int i = 0; i < timeline.StarCount; i++)
                _starLights[firstStar + i] = timeline.GetStarLight(i, revealSeconds);
            for (int i = 0; i < timeline.LineCount; i++)
                _lineProgress[firstLine + i] = timeline.GetLineProgress(i, revealSeconds);
        }

        private void ApplyProgress(MaterialPropertyBlock block, float[] stepProgress, float alpha, int submesh) {
            block.SetFloatArray(STEP_PROGRESS_PROPERTY, stepProgress);
            block.SetFloat(FIGURE_ALPHA_PROPERTY, alpha);
            _meshRenderer.SetPropertyBlock(block, submesh);
        }

        private void DestroyMesh() {
            _meshFilter.sharedMesh = null;
            if (_mesh != null)
                Destroy(_mesh);
            _mesh = null;
        }

        private MaterialPropertyBlock CreateStarLook() {
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
            return block;
        }

        private MaterialPropertyBlock CreateLineLook() {
            MaterialPropertyBlock block = new();
            block.SetFloat(IS_LINE_PROPERTY, LINE_PART);
            block.SetColor(COLOR_PROPERTY, _constellationHighlightConfiguration.LineColor);
            block.SetFloat(LINE_WIDTH_PROPERTY, _constellationHighlightConfiguration.LineWidthPixels);
            block.SetFloat(LINE_INTENSITY_PROPERTY, _constellationHighlightConfiguration.LineIntensity);
            block.SetFloat(LINE_GLOW_INTENSITY_PROPERTY, _constellationHighlightConfiguration.LineGlowIntensity);
            block.SetFloat(LINE_END_FADE_PROPERTY, _constellationHighlightConfiguration.LineEndFadeDegrees);
            return block;
        }
    }
}
