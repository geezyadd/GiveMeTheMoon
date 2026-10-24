using UnityEngine;

namespace Features.LobbyModule.Scripts {
    // One installed module on the ship panel: its icon and name, whether it is broken and whether it just broke.
    public readonly struct ShipConditionModuleDisplay {
        public Sprite Icon { get; }
        public string Name { get; }
        public bool IsBroken { get; }
        public bool HasJustBroken { get; }

        public ShipConditionModuleDisplay(Sprite icon, string name, bool isBroken, bool hasJustBroken) {
            Icon = icon;
            Name = name;
            IsBroken = isBroken;
            HasJustBroken = hasJustBroken;
        }
    }
}
