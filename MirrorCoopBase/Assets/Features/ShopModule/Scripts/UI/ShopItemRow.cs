using System;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopItemRow : MonoBehaviour {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _typeLabel;
        [SerializeField] private Text _statsLabel;
        [SerializeField] private Text _descriptionLabel;
        [SerializeField] private Text _priceLabel;
        [SerializeField] private Image _icon;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Color _affordablePriceColor = Color.white;
        [SerializeField] private Color _unaffordablePriceColor = Color.red;

        private int _index;

        public event Action<int> OnBuyClicked;

        private void Awake() =>
            _buyButton.onClick.AddListener(OnBuyButtonClicked);

        private void OnDestroy() =>
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);

        public void Bind(int index, ShopItemDisplay display) {
            _index = index;
            _nameLabel.text = display.Name;
            _typeLabel.text = display.ModuleType;
            _statsLabel.text = display.Stats;
            _descriptionLabel.text = display.Description;
            _priceLabel.text = display.Price;
            _icon.sprite = display.Icon;
            _icon.enabled = display.Icon != null;
        }

        public void SetAffordable(bool isAffordable) {
            _buyButton.interactable = isAffordable;
            _priceLabel.color = isAffordable ? _affordablePriceColor : _unaffordablePriceColor;
        }

        private void OnBuyButtonClicked() =>
            OnBuyClicked?.Invoke(_index);
    }
}
