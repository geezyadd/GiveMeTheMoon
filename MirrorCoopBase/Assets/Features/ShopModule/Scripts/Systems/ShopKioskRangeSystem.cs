using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using UnityEngine;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Closes the open shop window once the local player may no longer use the kiosk: the ship left the station or
    // the player is too far from the kiosk, whatever moved them (walking, a fall, a push, the ship).
    public sealed class ShopKioskRangeSystem : ITickable {
        private readonly ShopModel _shopModel;
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IPlayerLifeQuery _playerLifeQuery;
        private readonly IShopAccessService _shopAccessService;

        public ShopKioskRangeSystem(
            ShopModel shopModel,
            IPlayerBodyRegistry playerBodyRegistry,
            IPlayerLifeQuery playerLifeQuery,
            IShopAccessService shopAccessService) {
            _shopModel = shopModel;
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeQuery = playerLifeQuery;
            _shopAccessService = shopAccessService;
        }

        public void Tick() {
            if (_shopModel.IsOpen == false)
                return;

            PlayerLifeBody player = _playerBodyRegistry.LocalBody;
            if (player == null)
                return;

            Vector3 position = player.transform.position;
            ShopAccessStatus status = _shopAccessService.Evaluate(
                position,
                _playerLifeQuery.LocalState == PlayerLifeState.Alive,
                ReadKioskPosition(position),
                ShopAccessRange.KeepOpen);
            if (status != ShopAccessStatus.Allowed)
                _shopModel.Close();
        }

        // No kiosk (the debug key opened the window) counts as standing at it; a pad and its kiosk are only
        // destroyed in flight, which the phase check already covers.
        private Vector3 ReadKioskPosition(Vector3 playerPosition) {
            Transform kiosk = _shopModel.Kiosk;
            return kiosk != null ? kiosk.position : playerPosition;
        }
    }
}
