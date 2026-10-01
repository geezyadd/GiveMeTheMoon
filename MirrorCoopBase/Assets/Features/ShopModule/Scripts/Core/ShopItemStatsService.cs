using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopItemStatsService : IShopItemStatsService {
        private readonly EngineCatalog _engineCatalog;

        public ShopItemStatsService(EngineCatalog engineCatalog) =>
            _engineCatalog = engineCatalog;

        public IReadOnlyList<ShopItemStat> GetStats(ShipItem item) {
            if (_engineCatalog.TryGet(item.View, out EngineCatalog.EngineStats engine) == false)
                return Array.Empty<ShopItemStat>();

            return new[] {
                new ShopItemStat(ShipStatType.Thrust, engine.Thrust),
                new ShopItemStat(ShipStatType.FlightSpeed, engine.FlightSpeed)
            };
        }
    }
}
