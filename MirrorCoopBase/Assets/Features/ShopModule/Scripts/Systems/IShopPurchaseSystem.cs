using Features.ShopModule.Scripts.Network;
using UnityEngine;

namespace Features.ShopModule.Scripts.Systems {
    public interface IShopPurchaseSystem {
        public void ServerPurchase(CrewWallet wallet, int entryIndex, Transform buyer);
    }
}
