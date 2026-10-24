using UnityEngine;

namespace Features.LobbyModule.Scripts {
    // The hold bar: the fill's right anchor is the hold progress.
    public sealed class HoldUseProgressView : HoldUseProgressViewBase {
        [SerializeField] private GameObject _content;
        [SerializeField] private RectTransform _fill;

        public override void SetProgressVisible(bool isVisible) =>
            _content.SetActive(isVisible);

        public override void SetProgress(float fraction) {
            Vector2 anchorMax = _fill.anchorMax;
            anchorMax.x = Mathf.Clamp01(fraction);
            _fill.anchorMax = anchorMax;
        }
    }
}
