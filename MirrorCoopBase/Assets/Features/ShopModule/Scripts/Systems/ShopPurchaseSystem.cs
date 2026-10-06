using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Systems {
    public sealed class ShopPurchaseSystem : IShopPurchaseSystem {
        private readonly IShopPurchaseService _shopPurchaseService;
        private readonly IShopItemSpawnService _shopItemSpawnService;
        private readonly IShopAccessService _shopAccessService;
        private readonly ShopVisitModel _shopVisitModel;

        public ShopPurchaseSystem(
            IShopPurchaseService shopPurchaseService,
            IShopItemSpawnService shopItemSpawnService,
            IShopAccessService shopAccessService,
            ShopVisitModel shopVisitModel) {
            _shopPurchaseService = shopPurchaseService;
            _shopItemSpawnService = shopItemSpawnService;
            _shopAccessService = shopAccessService;
            _shopVisitModel = shopVisitModel;
        }

        // The client is not trusted: its window may have stayed open, or the command was sent by hand.
        public void ServerPurchase(ICrewWallet wallet, int entryIndex, IShopBuyer buyer) {
            if (TryGetReachableVisit(buyer, out ShopKioskVisit visit) == false)
                return;

            ShopPurchaseResult result = _shopPurchaseService.Evaluate(wallet.Balance, entryIndex);
            if (result.Status != ShopPurchaseStatus.Success)
                return;

            if (wallet.ServerTrySpend(result.Entry.Price) == false)
                return;

            _shopItemSpawnService.SpawnAtDelivery(result.Entry.Item, visit.DeliveryPoint);
        }

        private bool TryGetReachableVisit(IShopBuyer buyer, out ShopKioskVisit visit) =>
            _shopVisitModel.TryGetVisit(buyer.Identity, out visit)
            && visit.Kiosk != null
            && _shopAccessService.Evaluate(buyer.Position, buyer.IsAlive, visit.Kiosk.position, ShopAccessRange.Purchase) == ShopAccessStatus.Allowed;
    }
}
