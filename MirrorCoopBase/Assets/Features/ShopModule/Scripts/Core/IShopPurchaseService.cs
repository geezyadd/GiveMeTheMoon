using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopPurchaseService {
        public ShopPurchaseResult Evaluate(long balance, int entryIndex);
    }
}
