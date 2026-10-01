using Features.ShipModule.Scripts;

namespace Features.ShopModule.Scripts.Data {
    public readonly struct ShopItemStat {
        public ShipStatType Type { get; }
        public float Value { get; }

        public ShopItemStat(ShipStatType type, float value) {
            Type = type;
            Value = value;
        }
    }
}
