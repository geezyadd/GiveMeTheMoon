using Features.ShopModule.Scripts.Configurations;

namespace Features.ShopModule.Scripts.Data {
    public readonly struct ShopPurchaseResult {
        public ShopPurchaseStatus Status { get; }
        public ShopCatalog.Entry Entry { get; }

        public ShopPurchaseResult(ShopPurchaseStatus status, ShopCatalog.Entry entry) {
            Status = status;
            Entry = entry;
        }
    }
}
