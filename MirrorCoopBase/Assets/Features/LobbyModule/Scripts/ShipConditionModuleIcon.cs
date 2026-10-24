using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    // One module tile of the ship panel: the icon (the name while a module has no icon), tinted by its condition; a
    // fresh break blinks the tile for a while.
    public sealed class ShipConditionModuleIcon : MonoBehaviour {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;
        [SerializeField] private Color _workingColor = new Color(0.2f, 0.8f, 0.35f, 0.85f);
        [SerializeField] private Color _brokenColor = new Color(0.95f, 0.15f, 0.1f, 0.95f);
        [SerializeField] private Color _blinkColor = new Color(1f, 0.85f, 0.2f, 1f);

        private float _blinkSecondsLeft;
        private float _blinksPerSecond;
        private bool _isBroken;

        private void Update() {
            if (_blinkSecondsLeft <= 0f)
                return;

            _blinkSecondsLeft -= Time.deltaTime;
            ApplyColor();
        }

        public void Show(ShipConditionModuleDisplay module, float blinkSeconds, float blinksPerSecond) {
            gameObject.SetActive(true);
            _icon.sprite = module.Icon;
            _icon.enabled = module.Icon != null;
            _label.text = module.Name;
            _label.enabled = module.Icon == null;
            _isBroken = module.IsBroken;
            _blinksPerSecond = blinksPerSecond;
            if (module.HasJustBroken)
                _blinkSecondsLeft = blinkSeconds;
            else if (module.IsBroken == false)
                _blinkSecondsLeft = 0f;

            ApplyColor();
        }

        public void Hide() =>
            gameObject.SetActive(false);

        private void ApplyColor() =>
            _background.color = IsBlinkOn() ? _blinkColor : (_isBroken ? _brokenColor : _workingColor);

        private bool IsBlinkOn() =>
            _blinkSecondsLeft > 0f && Mathf.FloorToInt(_blinkSecondsLeft * _blinksPerSecond * 2f) % 2 == 0;
    }
}
