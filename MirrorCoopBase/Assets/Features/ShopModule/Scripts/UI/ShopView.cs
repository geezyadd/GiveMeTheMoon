using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopView : ShopViewBase {
        [SerializeField] private ShopItemRow _rowTemplate;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Button _closeButton;

        private readonly List<ShopItemRow> _rows = new();

        protected override void OnEnable() {
            base.OnEnable();
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        protected override void OnDisable() {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            base.OnDisable();
        }

        public override void DisposeView() {
            foreach (ShopItemRow row in _rows)
                row.OnBuyClicked -= OnRowBuyClicked;

            base.DisposeView();
        }

        public override void SetItems(IReadOnlyList<ShopItemDisplay> items) {
            _rowTemplate.gameObject.SetActive(false);
            for (int i = 0; i < items.Count; i++) {
                ShopItemRow row = Instantiate(_rowTemplate, _content);
                row.gameObject.SetActive(true);
                row.Bind(i, items[i]);
                row.OnBuyClicked += OnRowBuyClicked;
                _rows.Add(row);
            }
        }

        public override void SetItemAffordable(int index, bool isAffordable) =>
            _rows[index].SetAffordable(isAffordable);

        private void OnRowBuyClicked(int index) =>
            InvokeBuyClicked(index);

        private void OnCloseButtonClicked() =>
            InvokeCloseClicked();
    }
}
