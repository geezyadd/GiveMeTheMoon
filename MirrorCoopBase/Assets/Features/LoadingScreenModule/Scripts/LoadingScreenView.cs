using UnityEngine;

namespace Features.LoadingScreenModule {
    // The covering canvas in GlobalScene: a black panel. The canvas is off while
    // uncovered and blocks clicks while shown at all.
    [DisallowMultipleComponent]
    public sealed class LoadingScreenView : MonoBehaviour {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Seconds from uncovered to fully covered.")]
        [SerializeField, Min(0f)] private float _fadeInSeconds = 0.6f;
        [Tooltip("Seconds from fully covered to uncovered.")]
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 1.5f;

        public float FadeInSeconds => _fadeInSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;

        public float Alpha {
            get => _canvasGroup.alpha;
            set {
                _canvasGroup.alpha = value;
                bool shown = value > 0f;
                _canvasGroup.blocksRaycasts = shown;
                _canvas.enabled = shown;
            }
        }

        private void Awake() {
            Alpha = 0f;
        }
    }
}
