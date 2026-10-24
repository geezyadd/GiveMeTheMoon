using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Repair;
using Features.ShopModule.Scripts.Configurations;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopPurchaseService : IShopPurchaseService {
        private readonly ShopCatalog _shopCatalog;

        public ShopPurchaseService(ShopCatalog shopCatalog) =>
            _shopCatalog = shopCatalog;

        public ShopPurchaseResult Evaluate(long balance, int entryIndex) {
            if (TryGetEntry(entryIndex, out long price, out GameObject itemPrefab) == false)
                return new ShopPurchaseResult(ShopPurchaseStatus.UnknownItem, 0, null);

            if (balance < price)
                return new ShopPurchaseResult(ShopPurchaseStatus.NotEnoughMoney, price, itemPrefab);

            return new ShopPurchaseResult(ShopPurchaseStatus.Success, price, itemPrefab);
        }

        public bool CanAfford(long balance, int entryIndex) =>
            Evaluate(balance, entryIndex).Status == ShopPurchaseStatus.Success;

        private bool TryGetEntry(int entryIndex, out long price, out GameObject itemPrefab) {
            price = 0;
            itemPrefab = null;
            if (entryIndex < 0 || entryIndex >= _shopCatalog.EntryCount)
                return false;

            int moduleCount = _shopCatalog.Modules.Count;
            if (entryIndex < moduleCount) {
                ModuleDefinition module = _shopCatalog.Modules[entryIndex];
                price = module.Price;
                itemPrefab = module.ItemPrefab.gameObject;
                return true;
            }

            RepairToolDefinition tool = _shopCatalog.Tools[entryIndex - moduleCount];
            price = tool.Price;
            itemPrefab = tool.ItemPrefab.gameObject;
            return true;
        }
    }
}
