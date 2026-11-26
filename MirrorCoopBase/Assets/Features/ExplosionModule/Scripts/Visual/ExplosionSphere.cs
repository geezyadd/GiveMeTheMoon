using Features.EffectPoolModule.Scripts;
using Features.ExplosionModule.Configuration;
using UnityEngine;

namespace Features.ExplosionModule.Visual {
    // A see-through sphere that grows with the wave's front, fades out once it reached the radius and turns itself off.
    // A stand-in until the explosion gets its art.
    public sealed class ExplosionSphere : MonoBehaviour, ISelfDisablingEffect {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _properties;
        private ExplosionPreset _preset;
        private float _elapsed;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(Vector3 center, ExplosionPreset preset) {
            _preset = preset;
            _elapsed = 0f;
            transform.SetPositionAndRotation(center, Quaternion.identity);
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
            ApplyColor(1f);
        }

        private void Update() {
            if (_preset == null) {
                gameObject.SetActive(false);
                return;
            }

            _elapsed += Time.deltaTime;
            float radius = Mathf.Min(_preset.Radius, _preset.WaveSpeed * _elapsed);
            transform.localScale = Vector3.one * (radius * 2f);

            float fadeElapsed = _elapsed - _preset.Radius / _preset.WaveSpeed;
            if (fadeElapsed <= 0f)
                return;

            float fadeSeconds = _preset.SphereFadeSeconds;
            float alphaShare = fadeSeconds > 0f ? 1f - fadeElapsed / fadeSeconds : 0f;
            if (alphaShare <= 0f) {
                gameObject.SetActive(false);
                return;
            }

            ApplyColor(alphaShare);
        }

        private void ApplyColor(float alphaShare) {
            if (_renderer == null)
                return;

            _properties ??= new MaterialPropertyBlock();
            Color color = _preset.SphereColor;
            color.a *= alphaShare;
            _renderer.GetPropertyBlock(_properties);
            _properties.SetColor(_baseColorId, color);
            _properties.SetColor(_colorId, color);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
