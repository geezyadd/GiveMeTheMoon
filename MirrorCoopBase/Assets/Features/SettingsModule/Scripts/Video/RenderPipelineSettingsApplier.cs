using System;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;
using Object = UnityEngine.Object;

namespace Features.SettingsModule.Video {
    // Shadow quality, MSAA and render scale from the player's settings into the render pipeline. It works on a runtime
    // copy of the project's URP asset (changing the asset itself in the editor would write into the asset file) and
    // puts the project's asset back on dispose.
    public sealed class RenderPipelineSettingsApplier : IInitializable, IDisposable {
        private const float PERCENT = 100f;

        private readonly SettingsModel _settingsModel;
        private readonly VideoSettingsConfiguration _configuration;

        private RenderPipelineAsset _projectQualityAsset;
        private UniversalRenderPipelineAsset _runtimeAsset;

        public RenderPipelineSettingsApplier(SettingsModel settingsModel, VideoSettingsConfiguration configuration) {
            _settingsModel = settingsModel;
            _configuration = configuration;
        }

        public void Initialize() {
            _projectQualityAsset = QualitySettings.renderPipeline;
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset projectAsset) {
                Debug.LogError("Settings: the current render pipeline is not URP; shadow, anti-aliasing and render scale settings are off.");
                return;
            }

            _runtimeAsset = Object.Instantiate(projectAsset);
            _runtimeAsset.name = projectAsset.name + " (Runtime)";
            QualitySettings.renderPipeline = _runtimeAsset;
            Apply();
            _settingsModel.OnValueChanged += OnValueChanged;
        }

        public void Dispose() {
            if (_runtimeAsset == null)
                return;

            _settingsModel.OnValueChanged -= OnValueChanged;
            QualitySettings.renderPipeline = _projectQualityAsset;
            Object.Destroy(_runtimeAsset);
            _runtimeAsset = null;
        }

        private void OnValueChanged(string key) {
            if (key == SettingKeys.SHADOW_QUALITY || key == SettingKeys.ANTI_ALIASING || key == SettingKeys.RENDER_SCALE)
                Apply();
        }

        private void Apply() {
            if (_configuration.TryGetShadowLevel(_settingsModel.GetChoice(SettingKeys.SHADOW_QUALITY), out ShadowQualityLevel level)) {
                _runtimeAsset.mainLightShadowmapResolution = level.ShadowmapResolution;
                _runtimeAsset.shadowDistance = level.ShadowDistance;
                _runtimeAsset.shadowCascadeCount = level.CascadeCount;
            }

            if (_configuration.TryGetAntiAliasingSamples(_settingsModel.GetChoice(SettingKeys.ANTI_ALIASING), out int samples))
                _runtimeAsset.msaaSampleCount = samples;

            _runtimeAsset.renderScale = _settingsModel.GetSlider(SettingKeys.RENDER_SCALE) / PERCENT;
        }
    }
}
