using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    // The ring around the crosshair: it fills up while the weapon in hand cools down and turns to the ready colour
    // when the next shot may fire.
    public sealed class HeldWeaponReadinessView : HeldWeaponReadinessViewBase {
        private const float READY = 1f;

        [SerializeField] private GameObject _content;
        [SerializeField] private Image _ring;
        [SerializeField] private Color _coolingColor = Color.white;
        [SerializeField] private Color _readyColor = Color.white;

        public override void SetReadinessVisible(bool isVisible) =>
            _content.SetActive(isVisible);

        public override void SetReadiness(float readiness) {
            _ring.fillAmount = Mathf.Clamp01(readiness);
            _ring.color = readiness >= READY ? _readyColor : _coolingColor;
        }
    }
}
