namespace Features.ShopModule.Scripts.Core {
    public interface IShopPurchaseSystem {
        public void ServerPurchase(ICrewWallet wallet, int entryIndex, IShopBuyer buyer);
    }
}
