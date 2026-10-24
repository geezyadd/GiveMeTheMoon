using System;

namespace Features.ShipModule.Scripts.Damage {
    // Server only, for later systems (sounds, quests, stats): a module broke (socket id), a breach opened (point id),
    // something was repaired by a player (kind and id). The landing repair and a ship reset send no events.
    public interface IShipDamageEvents {
        public event Action<string> OnModuleBroken;
        public event Action<string> OnHullBreached;
        public event Action<ShipRepairKind, string> OnRepaired;
    }
}
