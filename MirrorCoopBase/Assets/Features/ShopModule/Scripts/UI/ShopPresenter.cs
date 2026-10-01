using System.Collections.Generic;
using Features.MvpModule;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopPresenter : PresenterBehaviour<ShopViewBase> {
        private const string MODULE_TYPE_FORMAT = "Module: {0}";
        private const string PRICE_FORMAT = "{0:N0}";

        private readonly ShopCatalog _shopCatalog;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly IShopItemStatsService _shopItemStatsService;
        private readonly IShopPurchaseService _shopPurchaseService;
        private readonly ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private readonly ShopModel _shopModel;

        public ShopPresenter(
            ShopCatalog shopCatalog,
            IReadOnlyWalletModel walletModel,
            IShopItemStatsService shopItemStatsService,
            IShopPurchaseService shopPurchaseService,
            ShopPurchaseRequestEventClass shopPurchaseRequestEventClass,
            ShopModel shopModel) {
            _shopCatalog = shopCatalog;
            _walletModel = walletModel;
            _shopItemStatsService = shopItemStatsService;
            _shopPurchaseService = shopPurchaseService;
            _shopPurchaseRequestEventClass = shopPurchaseRequestEventClass;
            _shopModel = shopModel;
        }

        protected override void OnViewSet() {
            View.SetItems(CreateDisplays());
            View.OnBuyClicked += OnBuyClicked;
            View.OnCloseClicked += OnCloseClicked;
            _walletModel.OnBalanceChanged += OnBalanceChanged;
            OnBalanceChanged();
        }

        protected override void OnDisposed() {
            View.OnBuyClicked -= OnBuyClicked;
            View.OnCloseClicked -= OnCloseClicked;
            _walletModel.OnBalanceChanged -= OnBalanceChanged;
        }

        private IReadOnlyList<ShopItemDisplay> CreateDisplays() {
            List<ShopItemDisplay> displays = new(_shopCatalog.Entries.Count);
            foreach (ShopCatalog.Entry entry in _shopCatalog.Entries)
                displays.Add(new ShopItemDisplay(
                    entry.DisplayName,
                    string.Format(MODULE_TYPE_FORMAT, entry.Item.Type),
                    _shopItemStatsService.FormatStats(entry.Item),
                    entry.Description,
                    string.Format(PRICE_FORMAT, entry.Price),
                    entry.Icon));

            return displays;
        }

        private void OnBalanceChanged() {
            for (int i = 0; i < _shopCatalog.Entries.Count; i++)
                View.SetItemAffordable(i, _shopPurchaseService.CanAfford(_walletModel.Balance, i));
        }

        private void OnBuyClicked(int index) =>
            _shopPurchaseRequestEventClass.InvokePurchaseRequested(index);

        private void OnCloseClicked() =>
            _shopModel.Close();
    }
}
