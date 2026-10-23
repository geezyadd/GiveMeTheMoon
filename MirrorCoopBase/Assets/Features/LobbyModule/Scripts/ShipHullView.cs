using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    // The hull bar: the fill's right anchor is the hull share; a hit lights the flash overlay and fades it out.
    public sealed class ShipHullView : ShipHullViewBase {
        [SerializeField] private GameObject _content;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _label;
        [SerializeField] private Image _flash;

        private float _flashSeconds;
        private float _flashAlpha;
        private float _flashSecondsLeft;

        private void Update() {
            if (_flashSecondsLeft <= 0f)
                return;

            _flashSecondsLeft -= Time.deltaTime;
            ApplyFlash();
        }

        public override void SetHullVisible(bool isVisible) =>
            _content.SetActive(isVisible);

        public override void SetHull(float fraction, string text) {
            Vector2 anchorMax = _fill.anchorMax;
            anchorMax.x = Mathf.Clamp01(fraction);
            _fill.anchorMax = anchorMax;
            _label.text = text;
        }

        public override void PlayDamageFlash(float seconds, float alpha) {
            _flashSeconds = seconds;
            _flashAlpha = alpha;
            _flashSecondsLeft = seconds;
            ApplyFlash();
        }

        private void ApplyFlash() {
            Color color = _flash.color;
            color.a = _flashAlpha * Mathf.Clamp01(_flashSecondsLeft / _flashSeconds);
            _flash.color = color;
        }
    }
}
