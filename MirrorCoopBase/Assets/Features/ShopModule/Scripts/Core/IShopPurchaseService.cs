namespace Features.ShopModule.Scripts.Core {
    public interface IShopPurchaseService {
        public ShopPurchaseResult Evaluate(long balance, int entryIndex);
        public bool CanAfford(long balance, int entryIndex);
    }
}
