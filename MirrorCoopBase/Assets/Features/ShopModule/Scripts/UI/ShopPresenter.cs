using System.Collections.Generic;
using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Repair;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopPresenter : PresenterBehaviour<ShopViewBase> {
        private const string MODULE_TYPE_FORMAT = "Module: {0}";
        private const string TOOL_TYPE_FORMAT = "Tool: repairs {0}";
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
            List<ShopItemDisplay> displays = new(_shopCatalog.EntryCount);
            foreach (ModuleDefinition module in _shopCatalog.Modules)
                displays.Add(new ShopItemDisplay(
                    module.DisplayName,
                    string.Format(MODULE_TYPE_FORMAT, module.SlotType),
                    _shopItemStatsService.FormatStats(module),
                    module.Description,
                    string.Format(PRICE_FORMAT, module.Price),
                    module.Icon));

            foreach (RepairToolDefinition tool in _shopCatalog.Tools)
                displays.Add(new ShopItemDisplay(
                    tool.DisplayName,
                    string.Format(TOOL_TYPE_FORMAT, tool.Repairs),
                    string.Empty,
                    tool.Description,
                    string.Format(PRICE_FORMAT, tool.Price),
                    tool.Icon));

            return displays;
        }

        private void OnBalanceChanged() {
            for (int i = 0; i < _shopCatalog.EntryCount; i++)
                View.SetItemAffordable(i, _shopPurchaseService.CanAfford(_walletModel.Balance, i));
        }

        private void OnBuyClicked(int index) =>
            _shopPurchaseRequestEventClass.InvokePurchaseRequested(index);

        private void OnCloseClicked() =>
            _shopModel.Close();
    }
}
