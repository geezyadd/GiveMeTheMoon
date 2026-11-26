using UnityEngine;

namespace Features.ScreenShakeModule {
    // One kind of screen shake as data: how hard and how fast the camera noise shakes and how long it takes to fade
    // out. A new source brings its own profile asset.
    [CreateAssetMenu(fileName = "ScreenShakeProfile", menuName = "Screen Shake/Profile")]
    public sealed class ScreenShakeProfile : ScriptableObject {
        [Tooltip("The camera noise's amplitude gain at strength 1, right when the shake starts.")]
        [SerializeField, Min(0f)] private float _amplitude = 0.5f;
        [Tooltip("The camera noise's frequency gain while this shake is the strongest.")]
        [SerializeField, Min(0f)] private float _frequency = 1f;
        [Tooltip("Seconds the shake takes to fade to nothing, linearly.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.25f;

        public float Amplitude => _amplitude;
        public float Frequency => _frequency;
        public float Duration => _duration;
    }
}
