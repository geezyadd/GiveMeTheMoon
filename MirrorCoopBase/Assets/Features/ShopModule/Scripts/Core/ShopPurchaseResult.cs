using Features.GrabModule.Scripts;

namespace Features.ShopModule.Scripts.Core {
    public readonly struct ShopPurchaseResult {
        public ShopPurchaseStatus Status { get; }
        public long Price { get; }
        public Grabbable ItemPrefab { get; }

        public ShopPurchaseResult(ShopPurchaseStatus status, long price, Grabbable itemPrefab) {
            Status = status;
            Price = price;
            ItemPrefab = itemPrefab;
        }
    }
}
