using System.Collections.Generic;
using System.Text;
using Features.MvpModule;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopPresenter : PresenterBehaviour<ShopViewBase> {
        private const string MODULE_TYPE_FORMAT = "Module: {0}";
        private const string STAT_FORMAT = "{0}: {1:0.##}";
        private const string STAT_SEPARATOR = "   ";
        private const string NO_STATS_TEXT = "No flight stats";
        private const string PRICE_FORMAT = "{0:N0}";

        private readonly ShopCatalog _shopCatalog;
        private readonly IWalletModel _walletModel;
        private readonly IShopItemStatsService _shopItemStatsService;
        private readonly IShopPurchaseService _shopPurchaseService;
        private readonly ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private readonly ShopModel _shopModel;

        public ShopPresenter(
            ShopCatalog shopCatalog,
            IWalletModel walletModel,
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
                    FormatStats(_shopItemStatsService.GetStats(entry.Item)),
                    entry.Description,
                    string.Format(PRICE_FORMAT, entry.Price),
                    entry.Icon));

            return displays;
        }

        private static string FormatStats(IReadOnlyList<ShopItemStat> stats) {
            if (stats.Count == 0)
                return NO_STATS_TEXT;

            StringBuilder builder = new();
            foreach (ShopItemStat stat in stats) {
                if (builder.Length > 0)
                    builder.Append(STAT_SEPARATOR);

                builder.AppendFormat(STAT_FORMAT, stat.Type, stat.Value);
            }

            return builder.ToString();
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
