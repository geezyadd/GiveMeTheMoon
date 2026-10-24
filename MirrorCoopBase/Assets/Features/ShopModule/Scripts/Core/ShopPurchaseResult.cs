using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public readonly struct ShopPurchaseResult {
        public ShopPurchaseStatus Status { get; }
        public long Price { get; }
        public GameObject ItemPrefab { get; }

        public ShopPurchaseResult(ShopPurchaseStatus status, long price, GameObject itemPrefab) {
            Status = status;
            Price = price;
            ItemPrefab = itemPrefab;
        }
    }
}
