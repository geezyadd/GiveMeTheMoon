using System;
using System.Collections.Generic;
using System.Text;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopItemStatsService : IShopItemStatsService {
        private const string STAT_FORMAT = "{0}: {1:0.##}";
        private const string STAT_SEPARATOR = "   ";
        private const string NO_STATS_TEXT = "No flight stats";

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

        public string FormatStats(ShipItem item) {
            IReadOnlyList<ShopItemStat> stats = GetStats(item);
            if (stats.Count == 0)
                return NO_STATS_TEXT;

            StringBuilder builder = new();
            foreach (ShopItemStat stat in stats) {
                if (builder.Length > 0)
                    builder.Append(STAT_SEPARATOR);

                builder.AppendFormat(STAT_FORMAT, stat.Type, stat.Value);
            }

            return builder.ToString();
        }
    }
}
