using Features.ShipModule.Scripts;

namespace Features.ShopModule.Scripts.Core {
    public readonly struct ShopPurchaseResult {
        public ShopPurchaseStatus Status { get; }
        public ModuleDefinition Module { get; }

        public ShopPurchaseResult(ShopPurchaseStatus status, ModuleDefinition module) {
            Status = status;
            Module = module;
        }
    }
}
