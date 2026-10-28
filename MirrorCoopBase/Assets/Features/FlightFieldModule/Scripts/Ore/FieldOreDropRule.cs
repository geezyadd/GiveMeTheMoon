using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // A small rock is the field's small size; a large one from the giant diameter is a giant; any other large rock is
    // big. The chance roll below the size's chance drops ore; the count roll spreads evenly over the size's counts.
    public sealed class FieldOreDropRule : IFieldOreDropRule {
        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;

        public FieldOreDropRule(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public int GetCount(in FieldRockDescription rock, float chanceRoll, float countRoll) {
            FieldOreDrop drop = GetDrop(rock);
            if (chanceRoll >= drop.Chance)
                return 0;

            int spread = drop.MaxCount - drop.MinCount + 1;
            return drop.MinCount + Mathf.Min(spread - 1, Mathf.FloorToInt(Mathf.Clamp01(countRoll) * spread));
        }

        private FieldOreDrop GetDrop(in FieldRockDescription rock) {
            if (rock.Size == FieldRockSize.Small)
                return _fieldRockDurabilityConfiguration.SmallRockOre;

            return rock.Diameter >= _fieldRockDurabilityConfiguration.GiantMinDiameterMeters
                ? _fieldRockDurabilityConfiguration.GiantRockOre
                : _fieldRockDurabilityConfiguration.BigRockOre;
        }
    }
}
