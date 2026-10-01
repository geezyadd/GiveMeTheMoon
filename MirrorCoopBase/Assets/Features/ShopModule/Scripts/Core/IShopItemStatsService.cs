using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopItemStatsService {
        public IReadOnlyList<ShopItemStat> GetStats(ShipItem item);
    }
}
