using System;
using UnityEngine;

namespace Features.SettingsModule.Video {
    // A shadow quality option of the video settings: the render pipeline's shadow values it sets.
    [Serializable]
    public sealed class ShadowQualityLevel {
        [SerializeField] private string _optionId;
        [Tooltip("The main light's shadow map size in pixels.")]
        [SerializeField] private int _shadowmapResolution = 2048;
        [Tooltip("How far from the camera shadows are drawn, in metres; 0 turns every shadow off.")]
        [SerializeField] private float _shadowDistance = 50f;
        [Tooltip("1 to 4 cascades.")]
        [SerializeField] private int _cascadeCount = 4;

        public string OptionId => _optionId;
        public int ShadowmapResolution => _shadowmapResolution;
        public float ShadowDistance => _shadowDistance;
        public int CascadeCount => _cascadeCount;
    }
}
