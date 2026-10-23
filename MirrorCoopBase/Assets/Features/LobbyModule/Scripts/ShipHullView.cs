using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    // The hull bar: the fill's right anchor is the hull share; a hit lights the flash overlay and fades it out.
    public sealed class ShipHullView : ShipHullViewBase {
        [SerializeField] private GameObject _content;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _label;
        [SerializeField] private Image _flash;
        [SerializeField, Min(0.01f)] private float _flashSeconds = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.85f;

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

        public override void PlayDamageFlash() {
            _flashSecondsLeft = _flashSeconds;
            ApplyFlash();
        }

        private void ApplyFlash() {
            Color color = _flash.color;
            color.a = _flashAlpha * Mathf.Clamp01(_flashSecondsLeft / _flashSeconds);
            _flash.color = color;
        }
    }
}
