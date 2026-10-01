using UnityEngine;

namespace Features.ShopModule.Scripts.UI {
    public readonly struct ShopItemDisplay {
        public string Name { get; }
        public string ModuleType { get; }
        public string Stats { get; }
        public string Description { get; }
        public string Price { get; }
        public Sprite Icon { get; }

        public ShopItemDisplay(string name, string moduleType, string stats, string description, string price, Sprite icon) {
            Name = name;
            ModuleType = moduleType;
            Stats = stats;
            Description = description;
            Price = price;
            Icon = icon;
        }
    }
}
