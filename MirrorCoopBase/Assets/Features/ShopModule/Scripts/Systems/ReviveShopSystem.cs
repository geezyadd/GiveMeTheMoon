using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Systems {
    // Server side of buying back a dead crewmate at the kiosk: the checks, then the payment, then the revive.
    public sealed class ReviveShopSystem : IReviveShopSystem {
        private readonly IReviveRule _reviveRule;
        private readonly IShopAccessService _shopAccessService;
        private readonly IDeadCrewService _deadCrewService;
        private readonly IPlayerLifeReviver _playerLifeReviver;
        private readonly ShopVisitModel _shopVisitModel;
        private readonly ReviveConfiguration _reviveConfiguration;

        public ReviveShopSystem(
            IReviveRule reviveRule,
            IShopAccessService shopAccessService,
            IDeadCrewService deadCrewService,
            IPlayerLifeReviver playerLifeReviver,
            ShopVisitModel shopVisitModel,
            ReviveConfiguration reviveConfiguration) {
            _reviveRule = reviveRule;
            _shopAccessService = shopAccessService;
            _deadCrewService = deadCrewService;
            _playerLifeReviver = playerLifeReviver;
            _shopVisitModel = shopVisitModel;
            _reviveConfiguration = reviveConfiguration;
        }

        // The client is not trusted: its window may be stale, two players may buy back the same crewmate at once, or
        // the command was sent by hand. Everything the revive needs is checked before paying, so no money is taken
        // for a revive that cannot happen.
        public void ServerRevive(ICrewWallet wallet, PlayerKey target, IShopBuyer buyer) {
            if (TryGetKioskVisit(buyer, out ShopKioskVisit visit) == false)
                return;

            long price = _reviveConfiguration.RevivePrice;
            ReviveRequest request = new(
                _shopAccessService.Evaluate(buyer.Position, buyer.IsAlive, visit.Kiosk.position, ShopAccessRange.Purchase),
                _deadCrewService.IsOnline(target),
                _deadCrewService.IsDead(target),
                wallet.Balance,
                price);
            if (_reviveRule.Evaluate(request) != ReviveStatus.Allowed)
                return;

            if (_playerLifeReviver.CanServerRevive(target) == false)
                return;

            if (wallet.ServerTrySpend(price) == false)
                return;

            _playerLifeReviver.ServerRevive(target);
        }

        private bool TryGetKioskVisit(IShopBuyer buyer, out ShopKioskVisit visit) =>
            _shopVisitModel.TryGetVisit(buyer.Identity, out visit) && visit.Kiosk != null;
    }
}
