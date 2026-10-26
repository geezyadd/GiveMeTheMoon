using System;
using Features.SonarModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SonarModule.Scripts.UI {
    // One echo on the scope: the kind's icon in its colour as big as the contact (never smaller than the kind's
    // icon), fading with the echo, a ring while the crew has it marked; a click asks to mark it.
    public sealed class SonarBlipButton : MonoBehaviour {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _markRing;

        private string _contactId;

        public event Action<string> OnClicked;

        public string ContactId => _contactId;

        private void OnEnable() =>
            _button.onClick.AddListener(OnButtonClicked);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnButtonClicked);

        public void Show(SonarBlip blip, float scopeRadiusPixels) {
            _contactId = blip.ContactId;
            gameObject.SetActive(true);
            _rect.anchoredPosition = blip.ScopePosition * scopeRadiusPixels;
            _rect.sizeDelta = Vector2.one * Mathf.Max(blip.Kind.IconPixels, blip.RadiusShare * scopeRadiusPixels * 2f);
            _icon.sprite = blip.Kind.Icon;
            Color color = blip.Kind.Color;
            color.a *= blip.Alpha;
            _icon.color = color;
            _markRing.enabled = blip.IsMarked;
            _markRing.color = color;
            _button.interactable = blip.Kind.IsMarkable;
        }

        public void Hide() =>
            gameObject.SetActive(false);

        private void OnButtonClicked() =>
            OnClicked?.Invoke(_contactId);
    }
}
