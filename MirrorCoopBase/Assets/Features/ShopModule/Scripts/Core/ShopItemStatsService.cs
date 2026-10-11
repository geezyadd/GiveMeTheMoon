using System;
using System.Collections.Generic;
using System.Text;
using Features.ShipModule.Scripts;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopItemStatsService : IShopItemStatsService {
        private const string FLAT_FORMAT = "{0} {1:+0.##;-0.##;+0}";
        private const string PERCENT_ADD_FORMAT = "{0} {1:+0.##;-0.##;+0}%";
        private const string PERCENT_MULTI_FORMAT = "{0} x{1:0.##}";
        private const float PERCENT_SCALE = 100f;
        private const string STAT_SEPARATOR = "   ";
        private const string NO_STATS_TEXT = "No flight stats";

        public string FormatStats(ModuleDefinition module) {
            IReadOnlyList<ModuleDefinition.StatModifierEntry> modifiers = module.StatModifiers;
            if (modifiers.Count == 0)
                return NO_STATS_TEXT;

            StringBuilder builder = new();
            for (int i = 0; i < modifiers.Count; i++) {
                if (builder.Length > 0)
                    builder.Append(STAT_SEPARATOR);

                builder.Append(FormatModifier(modifiers[i]));
            }

            return builder.ToString();
        }

        private static string FormatModifier(ModuleDefinition.StatModifierEntry modifier) =>
            modifier.Type switch {
                ModifierType.Flat => string.Format(FLAT_FORMAT, modifier.Stat, modifier.Value),
                ModifierType.PercentAdd => string.Format(PERCENT_ADD_FORMAT, modifier.Stat, modifier.Value * PERCENT_SCALE),
                ModifierType.PercentMulti => string.Format(PERCENT_MULTI_FORMAT, modifier.Stat, modifier.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier.Type, "Unknown stat modifier type.")
            };
    }
}
