using UnityEngine;
using UnityEngine.UI;

namespace Features.ShopModule.Scripts.UI {
    // The balance label; income makes it flash in a colour and grow a little, then fade back.
    public sealed class BalanceView : BalanceViewBase {
        private const string BALANCE_FORMAT = "Balance: {0:N0}";

        [SerializeField] private Text _label;

        private Color _baseColor;
        private Color _flashColor;
        private float _flashScale;
        private float _flashSeconds;
        private float _flashSecondsLeft;

        private void Awake() =>
            _baseColor = _label.color;

        private void Update() {
            if (_flashSecondsLeft <= 0f)
                return;

            _flashSecondsLeft -= Time.deltaTime;
            ApplyFlash();
        }

        public override void SetBalance(long balance) =>
            _label.text = string.Format(BALANCE_FORMAT, balance);

        public override void PlayIncomeFlash(float seconds, Color color, float scale) {
            _flashSeconds = seconds;
            _flashColor = color;
            _flashScale = scale;
            _flashSecondsLeft = seconds;
            ApplyFlash();
        }

        private void ApplyFlash() {
            float share = Mathf.Clamp01(_flashSecondsLeft / _flashSeconds);
            _label.color = Color.Lerp(_baseColor, _flashColor, share);
            _label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, _flashScale, share);
        }
    }
}
