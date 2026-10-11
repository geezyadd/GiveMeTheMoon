using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopPurchaseService : IShopPurchaseService {
        private readonly ShopCatalog _shopCatalog;

        public ShopPurchaseService(ShopCatalog shopCatalog) =>
            _shopCatalog = shopCatalog;

        public ShopPurchaseResult Evaluate(long balance, int entryIndex) {
            if (entryIndex < 0 || entryIndex >= _shopCatalog.Modules.Count)
                return new ShopPurchaseResult(ShopPurchaseStatus.UnknownItem, null);

            ModuleDefinition module = _shopCatalog.Modules[entryIndex];
            if (balance < module.Price)
                return new ShopPurchaseResult(ShopPurchaseStatus.NotEnoughMoney, module);

            return new ShopPurchaseResult(ShopPurchaseStatus.Success, module);
        }

        public bool CanAfford(long balance, int entryIndex) =>
            Evaluate(balance, entryIndex).Status == ShopPurchaseStatus.Success;
    }
}
