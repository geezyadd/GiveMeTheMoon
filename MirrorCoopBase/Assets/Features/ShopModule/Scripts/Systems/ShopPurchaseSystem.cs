using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Network;
using UnityEngine;

namespace Features.ShopModule.Scripts.Systems {
    public sealed class ShopPurchaseSystem : IShopPurchaseSystem {
        private readonly IShopPurchaseService _shopPurchaseService;
        private readonly IShopItemSpawnService _shopItemSpawnService;

        public ShopPurchaseSystem(IShopPurchaseService shopPurchaseService, IShopItemSpawnService shopItemSpawnService) {
            _shopPurchaseService = shopPurchaseService;
            _shopItemSpawnService = shopItemSpawnService;
        }

        public void ServerPurchase(CrewWallet wallet, int entryIndex, Transform buyer) {
            ShopPurchaseResult result = _shopPurchaseService.Evaluate(wallet.Balance, entryIndex);
            if (result.Status != ShopPurchaseStatus.Success)
                return;

            if (wallet.ServerTrySpend(result.Entry.Price) == false)
                return;

            _shopItemSpawnService.SpawnNear(result.Entry.Item, buyer);
        }
    }
}
