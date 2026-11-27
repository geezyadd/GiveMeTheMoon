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
        private static readonly int COLOR_PROPERTY = Shader.PropertyToID("_Color");

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
            ApplyColor(ConstellationMeshBuilder.STAR_SUBMESH, _constellationHighlightConfiguration.StarColor);
            ApplyColor(ConstellationMeshBuilder.LINE_SUBMESH, _constellationHighlightConfiguration.LineColor);
        }

        private void OnDisable() {
            _meshFilter.sharedMesh = null;
            DestroyImmediate(_mesh);
        }

        private void ApplyColor(int submesh, Color color) {
            MaterialPropertyBlock block = new();
            block.SetColor(COLOR_PROPERTY, color);
            _meshRenderer.SetPropertyBlock(block, submesh);
        }
    }
}
