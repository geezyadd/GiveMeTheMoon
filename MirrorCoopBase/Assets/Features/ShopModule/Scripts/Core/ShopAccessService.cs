using System;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    // One place that decides whether a player may use a kiosk: opening the window, keeping it open and buying
    // differ only in the allowed distance.
    public sealed class ShopAccessService : IShopAccessService {
        private readonly ShipRunModel _shipRunModel;
        private readonly ShopKioskConfiguration _shopKioskConfiguration;
        private readonly IShopAccessRule _shopAccessRule;

        public ShopAccessService(
            ShipRunModel shipRunModel,
            ShopKioskConfiguration shopKioskConfiguration,
            IShopAccessRule shopAccessRule) {
            _shipRunModel = shipRunModel;
            _shopKioskConfiguration = shopKioskConfiguration;
            _shopAccessRule = shopAccessRule;
        }

        public ShopAccessStatus Evaluate(Vector3 playerPosition, bool isAlive, Vector3 kioskPosition, ShopAccessRange range) =>
            _shopAccessRule.Evaluate(
                _shipRunModel.Phase == ShipRunPhase.Build,
                isAlive,
                Vector3.Distance(playerPosition, kioskPosition),
                ReadMaxDistance(range));

        private float ReadMaxDistance(ShopAccessRange range) =>
            range switch {
                ShopAccessRange.Open => _shopKioskConfiguration.OpenDistance,
                ShopAccessRange.KeepOpen => _shopKioskConfiguration.KeepOpenDistance,
                ShopAccessRange.Purchase => _shopKioskConfiguration.PurchaseDistance,
                _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
            };
    }
}
