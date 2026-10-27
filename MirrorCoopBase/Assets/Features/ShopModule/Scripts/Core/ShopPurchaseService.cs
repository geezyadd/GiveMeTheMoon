using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    // Shop entry indices: the catalog's modules first, then its tools, then its hand weapons; every bought item is a
    // grabbable prefab.
    public sealed class ShopPurchaseService : IShopPurchaseService {
        private readonly ShopCatalog _shopCatalog;

        public ShopPurchaseService(ShopCatalog shopCatalog) =>
            _shopCatalog = shopCatalog;

        public ShopPurchaseResult Evaluate(long balance, int entryIndex) {
            if (TryGetEntry(entryIndex, out long price, out Component item) == false)
                return new ShopPurchaseResult(ShopPurchaseStatus.UnknownItem, 0, null);

            Grabbable itemPrefab = item.GetComponent<Grabbable>();
            if (balance < price)
                return new ShopPurchaseResult(ShopPurchaseStatus.NotEnoughMoney, price, itemPrefab);

            return new ShopPurchaseResult(ShopPurchaseStatus.Success, price, itemPrefab);
        }

        public bool CanAfford(long balance, int entryIndex) =>
            TryGetEntry(entryIndex, out long price, out _) && balance >= price;

        private bool TryGetEntry(int entryIndex, out long price, out Component item) {
            price = 0;
            item = null;
            if (entryIndex < 0 || entryIndex >= _shopCatalog.EntryCount)
                return false;

            int moduleCount = _shopCatalog.Modules.Count;
            if (entryIndex < moduleCount) {
                ModuleDefinition module = _shopCatalog.Modules[entryIndex];
                price = module.Price;
                item = module.ItemPrefab;
                return true;
            }

            int toolIndex = entryIndex - moduleCount;
            if (toolIndex < _shopCatalog.Tools.Count) {
                RepairToolDefinition tool = _shopCatalog.Tools[toolIndex];
                price = tool.Price;
                item = tool.ItemPrefab;
                return true;
            }

            HandWeaponDefinition handWeapon = _shopCatalog.HandWeapons[toolIndex - _shopCatalog.Tools.Count];
            price = handWeapon.Price;
            item = handWeapon.ItemPrefab;
            return true;
        }
    }
}
