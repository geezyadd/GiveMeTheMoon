using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.Core {
    public interface IReviveShopSystem {
        public void ServerRevive(ICrewWallet wallet, PlayerKey target, IShopBuyer buyer);
    }
}
