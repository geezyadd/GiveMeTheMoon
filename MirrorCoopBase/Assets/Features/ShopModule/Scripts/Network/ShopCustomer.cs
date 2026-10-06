using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShopModule.Scripts.Network {
    // The player's network side of the shop kiosk: asks the access service, records the visit on the server and
    // opens the shop window on this player's own peer.
    public sealed class ShopCustomer : NetworkBehaviour, IShopBuyer {
        [SerializeField] private PlayerLifeBody _life;

        private ShopModel _shopModel;
        private ShopVisitModel _shopVisitModel;
        private IShopAccessService _shopAccessService;

        public NetworkIdentity Identity =>
            netIdentity;

        public Vector3 Position =>
            transform.position;

        public bool IsAlive =>
            _life.LifeState == PlayerLifeState.Alive;

        [Inject]
        private void InjectDependencies(ShopModel shopModel, ShopVisitModel shopVisitModel, IShopAccessService shopAccessService) {
            _shopModel = shopModel;
            _shopVisitModel = shopVisitModel;
            _shopAccessService = shopAccessService;
        }

        public override void OnStopServer() {
            _shopVisitModel.RemoveVisit(netIdentity);
            base.OnStopServer();
        }

        public bool CanOpenShopAt(Transform kiosk) =>
            _shopAccessService.Evaluate(Position, IsAlive, kiosk.position, ShopAccessRange.Open) == ShopAccessStatus.Allowed;

        [Server]
        public void ServerOpenShop(ShopKioskInteractable kiosk) {
            if (CanOpenShopAt(kiosk.transform) == false)
                return;

            _shopVisitModel.SetVisit(netIdentity, new ShopKioskVisit(kiosk.transform, kiosk.DeliveryPoint));
            TargetOpenShop(kiosk);
        }

        // The kiosk is null here when its pad was destroyed before the message arrived.
        [TargetRpc]
        private void TargetOpenShop(ShopKioskInteractable kiosk) {
            if (kiosk != null)
                _shopModel.Open(kiosk.transform);
        }
    }
}
