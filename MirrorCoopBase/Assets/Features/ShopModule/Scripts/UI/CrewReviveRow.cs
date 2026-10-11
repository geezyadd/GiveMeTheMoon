using System;
using Features.NetworkModelModule.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ShopModule.Scripts.UI {
    public sealed class CrewReviveRow : MonoBehaviour {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _priceLabel;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Color _affordablePriceColor = Color.white;
        [SerializeField] private Color _unaffordablePriceColor = Color.red;

        private PlayerKey _player;

        public event Action<PlayerKey> OnReviveClicked;

        private void Awake() =>
            _reviveButton.onClick.AddListener(OnReviveButtonClicked);

        private void OnDestroy() =>
            _reviveButton.onClick.RemoveListener(OnReviveButtonClicked);

        public void Bind(CrewReviveDisplay display) {
            _player = display.Player;
            _nameLabel.text = display.Name;
            _priceLabel.text = display.Price;
            _reviveButton.interactable = display.CanRevive;
            _priceLabel.color = display.CanRevive ? _affordablePriceColor : _unaffordablePriceColor;
        }

        private void OnReviveButtonClicked() =>
            OnReviveClicked?.Invoke(_player);
    }
}
