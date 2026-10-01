using Features.ShopModule.Scripts.Configurations;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopPurchaseService : IShopPurchaseService {
        private readonly ShopCatalog _shopCatalog;

        public ShopPurchaseService(ShopCatalog shopCatalog) =>
            _shopCatalog = shopCatalog;

        public ShopPurchaseResult Evaluate(long balance, int entryIndex) {
            if (entryIndex < 0 || entryIndex >= _shopCatalog.Entries.Count)
                return new ShopPurchaseResult(ShopPurchaseStatus.UnknownItem, null);

            ShopCatalog.Entry entry = _shopCatalog.Entries[entryIndex];
            if (balance < entry.Price)
                return new ShopPurchaseResult(ShopPurchaseStatus.NotEnoughMoney, entry);

            return new ShopPurchaseResult(ShopPurchaseStatus.Success, entry);
        }

        public bool CanAfford(long balance, int entryIndex) =>
            Evaluate(balance, entryIndex).Status == ShopPurchaseStatus.Success;
    }
}
